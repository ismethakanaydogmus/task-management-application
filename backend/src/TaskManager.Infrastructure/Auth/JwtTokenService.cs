using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Application.Abstractions;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Auth;

internal sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider clock) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    // JsonWebTokenHandler: eski JwtSecurityTokenHandler'ın yerine gelen, daha hızlı ve önerilen sınıf.
    private static readonly JsonWebTokenHandler Handler = new();

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_options.RefreshTokenDays);

    public AccessToken CreateAccessToken(User user)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_options.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            // CLAIM'ler: token'ın "iddiaları". Payload'a yazılır, imzalanır ama ŞİFRELENMEZ (herkes okuyabilir).
            // Bu yüzden sadece gerekli ve gizli olmayan bilgiler: kullanıcı id'si ve e-posta.
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),   // "sub" = subject = kim
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // token'ın benzersiz kimliği
            ]),
            SigningCredentials = new SigningCredentials(_options.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(Handler.CreateToken(descriptor), expires);
    }

    public string GenerateRefreshToken()
    {
        // Kriptografik olarak güvenli rastgele 64 bayt (512 bit). Random/Guid KULLANILMAZ:
        // System.Random tahmin edilebilir, Guid ise güvenlik için tasarlanmamıştır.
        return Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
    }

    public string HashRefreshToken(string refreshToken)
    {
        // Şifrelerin aksine burada YAVAŞ hash gerekmez: girdi 512 bitlik rastgele bir değer, tahmin edilemez.
        // Hızlı SHA-256 yeterli ve her refresh isteğinde ucuz.
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(hash);
    }
}
