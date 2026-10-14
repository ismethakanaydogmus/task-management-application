using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskManager.Application.Auth;
using TaskManager.IntegrationTests.Infrastructure;

namespace TaskManager.IntegrationTests;

// Kimlik doğrulama akışı uçtan uca: gerçek JWT, gerçek PBKDF2, gerçek httpOnly cookie, gerçek DB.
[Collection(ApiCollection.Name)]
public class AuthFlowTests(ApiFactory factory)
{
    private const string Password = "test-sifresi-123";

    [Fact]
    public async Task Login_SetsSecureHttpOnlyRefreshCookie_AndTokenWorks()
    {
        var email = await RegisterAsync();
        var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, Password));
        response.EnsureSuccessStatusCode();

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("refresh_token="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);   // JS okuyamaz (XSS)
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);     // sadece HTTPS
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase); // CSRF
        Assert.Contains("path=/api/auth", cookie, StringComparison.OrdinalIgnoreCase);

        var auth = await response.ReadAsync<AuthResponse>();
        var me = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        me.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        var meResponse = await client.SendAsync(me);
        Assert.Equal(email, (await meResponse.ReadAsync<UserResponse>())!.Email);
    }

    [Fact]
    public async Task TamperedToken_IsRejected()
    {
        var (client, auth) = await factory.CreateUserClientAsync("tamper");
        var parts = auth.AccessToken.Split('.');
        var tampered = $"{parts[0]}.{parts[1]}x.{parts[2]}"; // payload değişti → imza tutmaz

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tampered);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/tasks")).StatusCode);
    }

    [Fact]
    public async Task WrongPassword_AndUnknownEmail_ReturnSameResponse()
    {
        var email = await RegisterAsync();
        var client = factory.CreateHttpsClient();

        var wrongPassword = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "yanlis-sifre"));
        var unknownEmail = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest($"yok-{Guid.NewGuid():N}@test.com", Password));

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal((await wrongPassword.ReadAsync<ProblemResponse>())!.Detail, (await unknownEmail.ReadAsync<ProblemResponse>())!.Detail);
    }

    [Fact]
    public async Task Refresh_WithCookie_Rotates_AndReusingOldCookieKillsAllSessions()
    {
        var email = await RegisterAsync();

        // Cookie'leri ELLE yönetiyoruz ki "eski" cookie'yi saklayıp tekrar gönderebilelim (çalınmış token senaryosu).
        var client = factory.CreateHttpsClient(handleCookies: false);
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, Password));
        var oldCookie = ExtractRefreshCookie(login);

        var refresh = await PostRefreshAsync(client, oldCookie);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var newCookie = ExtractRefreshCookie(refresh);
        Assert.NotEqual(oldCookie, newCookie); // ROTATION

        // Eski cookie tekrar kullanıldı → reuse detection → 401
        Assert.Equal(HttpStatusCode.Unauthorized, (await PostRefreshAsync(client, oldCookie)).StatusCode);

        // ...ve meşru kullanıcının YENİ cookie'si de artık geçersiz (tüm oturumlar kapatıldı)
        Assert.Equal(HttpStatusCode.Unauthorized, (await PostRefreshAsync(client, newCookie)).StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken_AndDeletesCookie()
    {
        var email = await RegisterAsync();
        var client = factory.CreateHttpsClient(handleCookies: false);
        var cookie = ExtractRefreshCookie(await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, Password)));

        var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logout.Headers.Add("Cookie", $"refresh_token={cookie}");
        var logoutResponse = await client.SendAsync(logout);

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.Contains(logoutResponse.Headers.GetValues("Set-Cookie"), c => c.StartsWith("refresh_token=;")); // tarayıcıda silinir
        Assert.Equal(HttpStatusCode.Unauthorized, (await PostRefreshAsync(client, cookie)).StatusCode);         // sunucuda iptal
    }

    [Fact]
    public async Task Register_InvalidInput_ReturnsFieldErrors()
    {
        var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("bozuk", "123"));
        var problem = await response.ReadAsync<ProblemResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("email", problem!.Errors!.Keys);
        Assert.Contains("password", problem.Errors.Keys);
    }

    private async Task<string> RegisterAsync()
    {
        var email = $"auth-{Guid.NewGuid():N}@test.com";
        (await factory.CreateHttpsClient().PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, Password)))
            .EnsureSuccessStatusCode();
        return email;
    }

    private static Task<HttpResponseMessage> PostRefreshAsync(HttpClient client, string cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"refresh_token={cookie}");
        return client.SendAsync(request);
    }

    private static string ExtractRefreshCookie(HttpResponseMessage response)
    {
        var header = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("refresh_token="));
        return header["refresh_token=".Length..header.IndexOf(';')];
    }
}
