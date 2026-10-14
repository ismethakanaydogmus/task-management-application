using FluentValidation;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Auth;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResult> RefreshAsync(string? refreshToken, CancellationToken ct = default);
    Task LogoutAsync(string? refreshToken, CancellationToken ct = default);
    Task<UserResponse> GetUserAsync(int userId, CancellationToken ct = default);
}

internal sealed class AuthService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    TimeProvider clock,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    ILogger<AuthService> logger) : IAuthService
{
    // Yanlış e-posta ve yanlış şifre için AYNI mesaj.
    // "Bu e-posta kayıtlı değil" deseydik saldırgan hangi e-postaların sistemde olduğunu öğrenirdi (user enumeration).
    private const string InvalidCredentialsMessage = "E-posta veya şifre hatalı.";

    // Timing attack koruması için kullanılan sahte hash (aşağıda LoginAsync'e bak).
    private static string? _dummyPasswordHash;

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        await registerValidator.ValidateAndThrowAsync(request, ct);

        var email = NormalizeEmail(request.Email);

        // Ön kontrol: kullanıcıya düzgün bir 409 mesajı vermek için.
        // Yine de iki istek AYNI ANDA gelirse ikisi de bu kontrolü geçebilir (race condition).
        // Asıl garanti veritabanındaki UNIQUE index; repository o durumu da ConflictException'a çeviriyor.
        if (await users.EmailExistsAsync(email, ct))
            throw new ConflictException("Bu e-posta adresi zaten kayıtlı.");

        var user = new User
        {
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            CreatedAt = Now()
        };
        await users.AddAsync(user, ct);

        logger.LogInformation("Yeni kullanıcı kaydı: {UserId}", user.Id); // e-postayı log'lamıyoruz (kişisel veri)

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        await loginValidator.ValidateAndThrowAsync(request, ct);

        var user = await users.GetByEmailAsync(NormalizeEmail(request.Email), ct);

        if (user is null)
        {
            // TIMING ATTACK koruması: Kullanıcı yoksa hemen dönseydik cevap ~1 ms, varsa (hash doğrulama yavaş)
            // ~100 ms sürerdi. Saldırgan sadece cevap SÜRESİNE bakarak hangi e-postaların kayıtlı olduğunu anlardı.
            // Sahte bir hash'i doğrulayarak iki durumun süresini eşitliyoruz.
            _dummyPasswordHash ??= passwordHasher.Hash("dummy-password-for-timing");
            passwordHasher.Verify(_dummyPasswordHash, request.Password);
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        if (!passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            logger.LogWarning("Hatalı şifre denemesi: {UserId}", user.Id);
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResult> RefreshAsync(string? refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(refreshToken))
            throw new UnauthorizedException("Oturum bulunamadı.");

        var stored = await refreshTokens.GetByHashAsync(tokenService.HashRefreshToken(refreshToken), ct);
        if (stored is null)
            throw new UnauthorizedException("Oturum geçersiz.");

        var now = Now();

        // REUSE DETECTION: Bu token daha önce kullanılıp yenisiyle DEĞİŞTİRİLMİŞ (rotation) ama biri onu
        // tekrar gönderiyor. Meşru kullanıcı artık YENİ token'a sahip; eskisini gönderen büyük ihtimalle
        // token'ı çalmış biri. Hangisinin saldırgan olduğunu bilemeyiz → kullanıcının TÜM oturumlarını kapat.
        if (stored.RevokedAt is not null && stored.ReplacedByTokenHash is not null)
        {
            logger.LogWarning("İptal edilmiş refresh token tekrar kullanıldı; tüm oturumlar kapatılıyor: {UserId}", stored.UserId);
            await refreshTokens.RevokeAllForUserAsync(stored.UserId, now, ct);
            throw new UnauthorizedException("Oturum geçersiz. Lütfen tekrar giriş yapın.");
        }

        if (!stored.IsActive(now))
            throw new UnauthorizedException("Oturumun süresi doldu. Lütfen tekrar giriş yapın.");

        // ROTATION: Eski token'ı iptal et, yerine yenisini ver. Böylece her refresh token SADECE BİR KEZ kullanılabilir.
        // Not: stored "tracked"; buradaki değişiklik IssueTokensAsync içindeki AddAsync → SaveChanges ile
        // yeni token'la birlikte AYNI transaction'da yazılır (biri yazılıp diğeri yazılmamış durumu oluşmaz).
        var result = await IssueTokensAsync(stored.User, ct, onBeforeSave: newHash =>
        {
            stored.RevokedAt = now;
            stored.ReplacedByTokenHash = newHash;
        });

        return result;
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken ct = default)
    {
        // Çıkış her zaman "başarılı"dır (idempotent): token yoksa veya zaten iptalse yapacak bir şey yok.
        if (string.IsNullOrEmpty(refreshToken))
            return;

        var stored = await refreshTokens.GetByHashAsync(tokenService.HashRefreshToken(refreshToken), ct);
        if (stored is null || stored.RevokedAt is not null)
            return;

        stored.RevokedAt = Now();
        await refreshTokens.SaveChangesAsync(ct);

        // NOT: Access token (JWT) iptal EDİLEMEZ; süresi (15 dk) dolana kadar geçerli kalır.
        // Bu yüzden ömrü kısa tutuluyor. Frontend onu bellekten siliyor.
    }

    public async Task<UserResponse> GetUserAsync(int userId, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("Kullanıcı", userId);
        return new UserResponse(user.Id, user.Email);
    }

    private async Task<AuthResult> IssueTokensAsync(User user, CancellationToken ct, Action<string>? onBeforeSave = null)
    {
        var accessToken = tokenService.CreateAccessToken(user);

        var rawRefreshToken = tokenService.GenerateRefreshToken();
        var refreshTokenHash = tokenService.HashRefreshToken(rawRefreshToken);
        var now = Now();

        onBeforeSave?.Invoke(refreshTokenHash);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash, // veritabanına HASH yazılıyor
            CreatedAt = now,
            ExpiresAt = now + tokenService.RefreshTokenLifetime
        };
        await refreshTokens.AddAsync(refreshToken, ct);

        return new AuthResult(
            new AuthResponse(accessToken.Token, accessToken.ExpiresAt, new UserResponse(user.Id, user.Email)),
            rawRefreshToken, // istemciye (cookie) HAM değer gidiyor
            refreshToken.ExpiresAt);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;
}
