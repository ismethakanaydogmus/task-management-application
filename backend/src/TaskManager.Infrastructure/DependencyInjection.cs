using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Application.Abstractions;
using TaskManager.Infrastructure.Auth;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Persistence.Repositories;

namespace TaskManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Connection string kodda/appsettings'te DEĞİL:
        //  - Geliştirmede: dotnet user-secrets (Api projesinde; proje klasörü dışında saklanır)
        //  - Canlıda:      ortam değişkeni ConnectionStrings__Default  (":" yerine "__")
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Default bulunamadı. 'dotnet user-secrets set \"ConnectionStrings:Default\" \"...\"' ile ayarla.");

        // AddDbContext, AppDbContext'i varsayılan olarak SCOPED kaydeder (her HTTP isteğine bir tane).
        services.AddDbContext<AppDbContext>(options =>
            options
                .UseNpgsql(connectionString, npgsql =>
                    // RESILIENCY: geçici bağlantı hatalarında (ağ kesintisi, DB yeniden başlıyor, bulutta
                    // failover) hemen 500 dönmek yerine artan aralıklarla (exponential backoff) tekrar dener.
                    // Kalıcı hatalarda (yanlış şifre, olmayan tablo) tekrar DENEMEZ; sadece geçici olanlarda.
                    // Dikkat: elle BeginTransaction kullanılırsa işlem execution strategy içine alınmalı.
                    npgsql.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(2),
                        errorCodesToAdd: null))
                // C#'ta "CreatedAt" → Postgres'te "created_at".
                // Postgres büyük harfli isimleri ancak tırnakla ("CreatedAt") tanır; elle SQL yazarken
                // her seferinde tırnak koymamak için snake_case kullanıyoruz.
                .UseSnakeCaseNamingConvention());

        services.AddScoped<ITaskRepository, EfTaskRepository>();
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();

        // ---------- Kimlik doğrulama ----------
        // ValidateOnStart: Ayarlar eksik/hatalıysa uygulama AÇILIRKEN çöker (fail fast).
        // Aksi halde uygulama sorunsuz açılır, ilk kullanıcı giriş yapmaya çalıştığında anlaşılmaz bir hata alırdık.
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer) && !string.IsNullOrWhiteSpace(o.Audience),
                "Jwt:Issuer ve Jwt:Audience ayarlanmalı.")
            // HMAC-SHA256 için anahtar en az 256 bit (32 bayt) olmalı. Kısa anahtar kaba kuvvetle bulunabilir.
            .Validate(o => System.Text.Encoding.UTF8.GetByteCount(o.SigningKey) >= 32,
                "Jwt:SigningKey en az 32 bayt olmalı. 'dotnet user-secrets set \"Jwt:SigningKey\" \"...\"' ile ayarla.")
            .ValidateOnStart();

        // Durumsuz (stateless) ve thread-safe → Singleton yeterli.
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();

        return services;
    }
}
