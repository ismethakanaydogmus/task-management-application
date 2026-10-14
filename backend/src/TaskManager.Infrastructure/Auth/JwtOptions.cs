using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TaskManager.Infrastructure.Auth;

// "Options pattern": appsettings'teki "Jwt" bölümü bu sınıfa bağlanır (Bind) ve DI ile IOptions<JwtOptions> olarak alınır.
// Ayarları string anahtarlarla (configuration["Jwt:Issuer"]) her yerde ayrı ayrı okumak yerine tek tip güvenli nesne.
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    // Token'ı KİM üretti? (bizim API'miz)
    public string Issuer { get; init; } = string.Empty;

    // Token KİMİN İÇİN üretildi? Başka bir servisin token'ı bizim API'mizde kabul edilmesin.
    public string Audience { get; init; } = string.Empty;

    // HMAC-SHA256 imza anahtarı. GİZLİ! appsettings'te DEĞİL, user-secrets / ortam değişkeninde.
    // Bu anahtarı bilen herkes istediği kullanıcı adına geçerli token üretebilir.
    public string SigningKey { get; init; } = string.Empty;

    // Kısa ömür: çalınırsa zarar penceresi küçük; JWT iptal edilemediği için asıl koruma bu.
    public int AccessTokenMinutes { get; init; } = 15;

    public int RefreshTokenDays { get; init; } = 7;

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));

    // Hem token ÜRETİRKEN hem DOĞRULARKEN aynı kurallar kullanılsın diye tek yerde.
    public TokenValidationParameters CreateValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = Issuer,
        ValidateAudience = true,
        ValidAudience = Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = CreateSigningKey(),
        ValidateLifetime = true,
        // Varsayılan ClockSkew 5 dakikadır (sunucu saatleri arasındaki farkı tolere etmek için).
        // 15 dakikalık token'a 5 dk ek süre çok fazla; 30 sn'ye indiriyoruz.
        ClockSkew = TimeSpan.FromSeconds(30),
        // Sadece HS256 kabul et. ("alg": "none" ile imzasız token gönderme saldırısına karşı)
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        NameClaimType = "email",
    };
}
