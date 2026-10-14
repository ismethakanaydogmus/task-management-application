using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace TaskManager.IntegrationTests.Infrastructure;

// INTEGRATION TEST ALTYAPISI
//
// WebApplicationFactory<Program>: API'nin GERÇEK Program.cs'ini test sürecinin içinde, bellekte (TestServer)
// ayağa kaldırır. Gerçek middleware pipeline, DI, JWT, validasyon, ProblemDetails... hepsi çalışır;
// sadece ağ portu açılmaz. Testler ona normal HttpClient ile istek atar.
//
// Testcontainers: Docker'da GEÇİCİ, TEMİZ bir PostgreSQL başlatır; testler bitince siler.
//  - Geliştirme veritabanına (5433) DOKUNULMAZ.
//  - Her makinede (ve CI'da) aynı sürüm: "bende çalışıyordu" yok.
//  - Neden EF InMemory provider değil? O gerçek bir veritabanı değil: ILIKE, unique constraint, foreign key,
//    transaction, CASE sıralaması... yok. Testler geçer, canlıda patlar.
//
// IAsyncLifetime: xUnit, testlerden ÖNCE InitializeAsync'i, SONRA DisposeAsync'i çağırır.
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:17-alpine") // geliştirmedeki sürümle AYNI
        .Build();

    public async Task InitializeAsync()
    {
        await _db.StartAsync();

        // Migration'ları BOŞ bir veritabanına uygula. Bu kendi başına bir testtir:
        // Aşama 8'de elle düzenlediğimiz migration dahil, tüm zincir sıfırdan çalışabiliyor mu?
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" ortamı: user-secrets (sadece Development'ta yüklenir) ve geliştirme ayarları devreye girmez.
        builder.UseEnvironment("Testing");

        // UseSetting: Program.cs çalışırken builder.Configuration'da bu değerler OLUR
        // (AddInfrastructure connection string'i servis kaydı sırasında okuyor).
        builder.UseSetting("ConnectionStrings:Default", _db.GetConnectionString());
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-en-az-32-bayt-uzunlugunda!!");
        builder.UseSetting("RateLimiting:AuthPermitLimit", "100000"); // testler çok kullanıcı kaydediyor
    }

    // WebApplicationFactory'nin kendi DisposeAsync'i (ValueTask) ile IAsyncLifetime'ınki (Task) çakışıyor → açık uygulama.
    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _db.DisposeAsync();
    }
}

// COLLECTION FIXTURE: Aynı ApiFactory'yi (= tek PostgreSQL container'ı) bu koleksiyondaki TÜM test sınıfları paylaşır.
// Her sınıf için ayrı container açsaydık her biri birkaç saniye sürerdi.
// Paylaşımın bedeli: testler aynı veritabanını kullanır → her test KENDİ benzersiz kullanıcısını oluşturur
// ve sadece onun verisine bakar, böylece birbirlerini etkilemezler (izolasyon veri seviyesinde).
// Alternatif: Her testten önce tabloları temizlemek (Respawn kütüphanesi).
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
