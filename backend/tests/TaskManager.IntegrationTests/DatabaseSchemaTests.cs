using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Infrastructure.Persistence;
using TaskManager.IntegrationTests.Infrastructure;

namespace TaskManager.IntegrationTests;

[Collection(ApiCollection.Name)]
public class DatabaseSchemaTests(ApiFactory factory)
{
    // "Modeli değiştirdim ama migration eklemeyi unuttum" hatasını yakalar.
    // Bu test olmasaydı: geliştirmede çalışır (belki elle tablo düzelttin), canlıda kolon yok → 500.
    [Fact]
    public void Model_HasNoChangesWithoutMigration()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(db.Database.HasPendingModelChanges(),
            "Model değişmiş ama migration yok: dotnet ef migrations add <Isim> ...");
    }

    [Fact]
    public async Task AllMigrations_AppliedToEmptyDatabase()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }
}
