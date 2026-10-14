using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Application.Abstractions;
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
                .UseNpgsql(connectionString)
                // C#'ta "CreatedAt" → Postgres'te "created_at".
                // Postgres büyük harfli isimleri ancak tırnakla ("CreatedAt") tanır; elle SQL yazarken
                // her seferinde tırnak koymamak için snake_case kullanıyoruz.
                .UseSnakeCaseNamingConvention());

        services.AddScoped<ITaskRepository, EfTaskRepository>();

        return services;
    }
}
