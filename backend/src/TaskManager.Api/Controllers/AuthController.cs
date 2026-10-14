using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;
using TaskManager.Application.Auth;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private const string RefreshCookieName = "refresh_token";

    // POST /api/auth/register
    [HttpPost("register")]
    [AllowAnonymous]                       // Fallback policy "giriş zorunlu" diyor; kayıt için istisna
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await authService.RegisterAsync(request, ct);
        SetRefreshCookie(result);
        return Ok(result.Response);
    }

    // POST /api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)] // Kaba kuvvet (brute force) şifre denemelerine karşı
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        SetRefreshCookie(result);
        return Ok(result.Response);
    }

    // POST /api/auth/refresh — gövde YOK; refresh token cookie'den okunur.
    // Access token'ın süresi dolunca veya sayfa yenilenince (bellekteki token kaybolunca) frontend bunu çağırır.
    [HttpPost("refresh")]
    [AllowAnonymous] // access token'ı zaten yok/süresi dolmuş olabilir; kimlik cookie'deki refresh token ile kanıtlanır
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken ct)
    {
        try
        {
            var result = await authService.RefreshAsync(Request.Cookies[RefreshCookieName], ct);
            SetRefreshCookie(result); // ROTATION: yeni refresh token eskisinin yerine yazılır
            return Ok(result.Response);
        }
        catch (Application.Common.Exceptions.UnauthorizedException)
        {
            // Geçersiz cookie'yi tarayıcıda bırakmayalım; sonra exception'ı handler'a (401) bırak.
            DeleteRefreshCookie();
            throw;
        }
    }

    // POST /api/auth/logout
    [HttpPost("logout")]
    [AllowAnonymous] // access token'ın süresi dolmuş olsa bile çıkış yapılabilmeli
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await authService.LogoutAsync(Request.Cookies[RefreshCookieName], ct);
        DeleteRefreshCookie();
        return NoContent();
    }

    // GET /api/auth/me — geçerli access token GEREKİR (fallback policy). Token'daki kullanıcıyı döner.
    [HttpGet("me")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken ct)
    {
        // User: JwtBearer middleware'inin token'dan oluşturduğu ClaimsPrincipal.
        // "sub" claim'i → kullanıcı id'si (MapInboundClaims=false olduğu için adı değişmeden "sub").
        var userId = int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        return Ok(await authService.GetUserAsync(userId, ct));
    }

    private void SetRefreshCookie(AuthResult result)
    {
        Response.Cookies.Append(RefreshCookieName, result.RefreshToken, CreateCookieOptions(result.RefreshTokenExpiresAt));
    }

    private void DeleteRefreshCookie()
    {
        // Silmek için de AYNI path/samesite/secure ile yazmak gerekir; yoksa tarayıcı farklı bir cookie sanar.
        Response.Cookies.Delete(RefreshCookieName, CreateCookieOptions(expires: null));
    }

    private CookieOptions CreateCookieOptions(DateTime? expires) => new()
    {
        // HttpOnly: JavaScript (document.cookie) bu cookie'yi OKUYAMAZ → XSS ile çalınamaz.
        HttpOnly = true,
        // Secure: sadece HTTPS üzerinden gönderilir. (Tarayıcılar http://localhost'u "güvenli" sayar, geliştirmede çalışır.)
        Secure = true,
        // SameSite=Strict: Başka bir SİTEDEN tetiklenen isteklerde gönderilmez → CSRF koruması.
        // localhost:5173 ile localhost:5263 "aynı site"dir (site = şema + alan adı; port önemsiz).
        // Canlıda app.ornek.com ile api.ornek.com da aynı site sayılır.
        SameSite = SameSiteMode.Strict,
        // Path: Cookie SADECE /api/auth/* isteklerinde gönderilir; görev isteklerinde gereksiz yere taşınmaz.
        Path = "/api/auth",
        Expires = expires,
        IsEssential = true,
    };
}

public static class RateLimitPolicies
{
    public const string Auth = "auth";
}
