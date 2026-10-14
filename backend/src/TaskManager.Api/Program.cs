// =====================================================================
// Program.cs — uygulamanın giriş noktası.
// İki aşaması vardır:
//   1) builder aşaması : servisleri DI (Dependency Injection) container'a KAYDEDERİZ.
//   2) app aşaması     : HTTP isteğinin geçeceği middleware PIPELINE'ını KURARIZ.
// Kural: builder.Build()'den önce "ne kullanılabilir" (servisler),
//        sonra "istek hangi sırayla işlenir" (middleware) tanımlanır.
// =====================================================================

using System.Text.Json.Serialization;
using TaskManager.Api.ErrorHandling;
using TaskManager.Application;
using TaskManager.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
// CreateBuilder şunları hazır getirir:
//  - Configuration: appsettings.json -> appsettings.{Environment}.json -> ortam değişkenleri -> komut satırı
//    (sonra gelen öncekini ezer)
//  - Logging: Console/Debug logger
//  - Kestrel: ASP.NET Core'un kendi web sunucusu

// ---------- 1) SERVİS KAYITLARI (DI container) ----------
builder.Services
    .AddControllers(options =>
    {
        // Nullable açıkken MVC, "string Title" gibi null olamayan parametreleri GİZLİCE [Required] sayar
        // ve kendi İngilizce mesajıyla ("The Title field is required.") 400 döner.
        // Bunu kapatıyoruz: tüm girdi kuralları TEK bir yerde, FluentValidation validator'larında olsun.
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddJsonOptions(options =>
    {
        // Enum'lar JSON'da sayı (2) yerine metin ("High") olarak yazılsın/okunsun.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Model binding hatalarını (bozuk JSON, geçersiz enum) FluentValidation hatalarıyla aynı formata getir.
        options.InvalidModelStateResponseFactory = InvalidModelStateResponse.Create;
    });
builder.Services.AddOpenApi();       // /openapi/v1.json adresinde API dokümanı üretir

// ProblemDetails (RFC 9457): TÜM hata yanıtları için ortak JSON formatı.
// Burada eklenen alanlar hem bizim GlobalExceptionHandler'ımızın hem de framework'ün
// kendi ürettiği hataların (404, model binding 400'ü) hepsine uygulanır.
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        // Hangi istekte hata oldu? (traceId zaten varsayılan olarak ekleniyor)
        context.ProblemDetails.Instance =
            $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
    };
});
// Yakalanmayan exception'ları ProblemDetails'e çeviren sınıfımız.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Her katman kendi servislerini kendi DependencyInjection.cs dosyasında kaydeder.
// Api projesi (composition root) sadece hepsini bir araya getirir.
builder.Services.AddApplication();                            // TaskService, validator'lar, TimeProvider
builder.Services.AddInfrastructure(builder.Configuration);    // AppDbContext (PostgreSQL), EfTaskRepository

var app = builder.Build();

// ---------- 2) MIDDLEWARE PIPELINE (sıra ÖNEMLİ!) ----------
// Her istek yukarıdan aşağı akar, yanıt aşağıdan yukarı geri döner.

// EN BAŞTA: altındaki tüm middleware'lerde ve controller'larda oluşan exception'ları yakalar.
// Sonda olsaydı, kendisinden önce çalışan middleware'lerin hatalarını göremezdi.
app.UseExceptionHandler();

// Gövdesi boş 4xx/5xx yanıtlarına (ör. olmayan bir URL → 404) da ProblemDetails gövdesi ekler.
// Böylece istemci HER hata yanıtında aynı formatı görür.
app.UseStatusCodePages();

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
