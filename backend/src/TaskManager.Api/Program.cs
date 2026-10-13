// =====================================================================
// Program.cs — uygulamanın giriş noktası.
// İki aşaması vardır:
//   1) builder aşaması : servisleri DI (Dependency Injection) container'a KAYDEDERİZ.
//   2) app aşaması     : HTTP isteğinin geçeceği middleware PIPELINE'ını KURARIZ.
// Kural: builder.Build()'den önce "ne kullanılabilir" (servisler),
//        sonra "istek hangi sırayla işlenir" (middleware) tanımlanır.
// =====================================================================

using System.Text.Json.Serialization;
using TaskManager.Api.Repositories;

var builder = WebApplication.CreateBuilder(args);
// CreateBuilder şunları hazır getirir:
//  - Configuration: appsettings.json -> appsettings.{Environment}.json -> ortam değişkenleri -> komut satırı
//    (sonra gelen öncekini ezer)
//  - Logging: Console/Debug logger
//  - Kestrel: ASP.NET Core'un kendi web sunucusu

// ---------- 1) SERVİS KAYITLARI (DI container) ----------
builder.Services
    .AddControllers()   // [ApiController] sınıflarını bulup kullanılabilir yapar
    .AddJsonOptions(options =>
    {
        // Enum'lar JSON'da sayı (2) yerine metin ("High") olarak yazılsın/okunsun.
        // Okunabilir ve güvenli: enum'a yeni değer eklenince sayılar kaysa bile frontend etkilenmez.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();       // /openapi/v1.json adresinde API dokümanı üretir

// "ITaskRepository isteyen herkese InMemoryTaskRepository ver."
// SINGLETON: veri bu nesnenin içinde (RAM'de) duruyor. Scoped olsaydı her HTTP isteğinde
// yeni ve BOŞ bir repository oluşur, eklediğimiz görevler bir sonraki istekte kaybolurdu.
// Aşama 2'de bu satır AddScoped<ITaskRepository, EfTaskRepository>() olacak.
builder.Services.AddSingleton<ITaskRepository, InMemoryTaskRepository>();

var app = builder.Build();

// ---------- 2) MIDDLEWARE PIPELINE (sıra ÖNEMLİ!) ----------
// Her istek yukarıdan aşağı akar, yanıt aşağıdan yukarı geri döner.
if (app.Environment.IsDevelopment())
{
    // API dokümanını sadece geliştirme ortamında açıyoruz:
    // canlı ortamda iç yapımızı dışarıya göstermek istemeyiz.
    app.MapOpenApi();
}

app.UseHttpsRedirection();   // http:// gelen isteği https://'e yönlendirir
app.UseAuthorization();      // [Authorize] attribute'larını kontrol eder (Aşama 7'de anlam kazanacak)
app.MapControllers();        // URL'yi doğru controller action'ına eşler (routing)

app.Run();
