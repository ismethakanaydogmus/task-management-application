// =====================================================================
// Program.cs — uygulamanın giriş noktası.
// İki aşaması vardır:
//   1) builder aşaması : servisleri DI (Dependency Injection) container'a KAYDEDERİZ.
//   2) app aşaması     : HTTP isteğinin geçeceği middleware PIPELINE'ını KURARIZ.
// Kural: builder.Build()'den önce "ne kullanılabilir" (servisler),
//        sonra "istek hangi sırayla işlenir" (middleware) tanımlanır.
// =====================================================================

using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using TaskManager.Api.Auth;
using TaskManager.Api.Controllers;
using TaskManager.Application.Abstractions;
using TaskManager.Api.ErrorHandling;
using TaskManager.Application;
using TaskManager.Infrastructure;
using TaskManager.Infrastructure.Auth;

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

// CORS: Tarayıcının Same-Origin Policy'sine KONTROLLÜ bir istisna.
// Frontend (localhost:5173) ile API (localhost:5263) farklı "origin" (şema + host + PORT) olduğu için
// tarayıcı, API cevabını okumaya ancak API "bu origin'e izin veriyorum" derse izin verir.
//
// İzinli adresler KODDA DEĞİL configuration'da:
//  - appsettings.Development.json → http://localhost:5173
//  - Canlıda ortam değişkeni      → Cors__AllowedOrigins__0=https://gorevler.example.com
// Liste boşsa hiçbir origin'e izin verilmez (güvenli varsayılan).
//
// AllowAnyOrigin() KULLANMIYORUZ: herhangi bir sitenin JavaScript'i API'mizi kullanıcı adına çağırabilirdi.
// (Ayrıca AllowAnyOrigin, AllowCredentials ile birlikte kullanılamaz; çerez tabanlı oturumda zaten çalışmaz.)
const string FrontendCorsPolicy = "Frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()                  // Content-Type, Authorization
        .AllowAnyMethod()                  // GET, POST, PUT, DELETE
        // AllowCredentials: Tarayıcı cross-origin isteklerde cookie'yi (refresh token) ancak bu izinle gönderir/kabul eder.
        // Frontend tarafında da fetch(..., { credentials: 'include' }) gerekir. İkisi birlikte olmazsa cookie yok sayılır.
        .AllowCredentials()
        .WithExposedHeaders("Location"));  // 201 cevabındaki Location header'ını JS okuyabilsin
});

// Her katman kendi servislerini kendi DependencyInjection.cs dosyasında kaydeder.
// Api projesi (composition root) sadece hepsini bir araya getirir.
builder.Services.AddApplication();                            // TaskService, AuthService, validator'lar, TimeProvider
builder.Services.AddInfrastructure(builder.Configuration);    // AppDbContext, repository'ler, JWT, şifre hash'leme

// "Bu isteği kim yapıyor?" → token'daki "sub" claim'i. Scoped: her HTTP isteği kendi kullanıcısını görür.
// (Singleton olsaydı... IHttpContextAccessor yine doğru isteği verirdi, ama Scoped niyeti daha açık ifade ediyor.)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

// ---------- AUTHENTICATION (Sen kimsin?) ----------
// JwtBearer: her istekte "Authorization: Bearer <token>" başlığını okur, imzayı/süreyi/issuer'ı doğrular,
// geçerliyse HttpContext.User'ı token'daki claim'lerle doldurur. Veritabanına GİTMEZ (stateless).
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // .NET varsayılan olarak "sub" gibi standart JWT claim adlarını uzun Microsoft URI'larına çevirir
        // ("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"). Kafa karıştırıcı; kapatıyoruz.
        options.MapInboundClaims = false;
    });

// Doğrulama kuralları (issuer, audience, imza anahtarı) JwtOptions'tan gelir — token ÜRETEN JwtTokenService ile AYNI kaynak.
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
        bearer.TokenValidationParameters = jwt.Value.CreateValidationParameters());

// ---------- AUTHORIZATION (Buna yetkin var mı?) ----------
// FALLBACK POLICY: [Authorize] veya [AllowAnonymous] yazılmamış HER endpoint "giriş yapmış kullanıcı" ister.
// "Varsayılan olarak güvenli" (secure by default): yeni bir controller eklerken [Authorize] yazmayı UNUTSAN bile
// endpoint korumalı olur. Herkese açık olması gerekenleri [AllowAnonymous] ile BİLİNÇLİ olarak açarız.
// Alternatif: her controller'a tek tek [Authorize] yazmak → birini unutmak = veri sızıntısı.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// ---------- RATE LIMITING ----------
// Giriş/kayıt endpoint'lerine aynı IP'den dakikada en fazla 10 istek.
// Olmasaydı: saldırgan saniyede yüzlerce şifre deneyebilirdi (brute force / credential stuffing).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimitPolicies.Auth, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            // Her IP adresi kendi "kovasına" sahip. (Reverse proxy arkasında gerçek IP için
            // ForwardedHeaders middleware'i gerekir — Aşama 10.)
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

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
    app.MapOpenApi().AllowAnonymous(); // fallback policy'den muaf: dokümanı görmek için giriş gerekmesin
}

app.UseHttpsRedirection();   // http:// gelen isteği https://'e yönlendirir

// CORS, routing'den sonra ve Authorization'dan ÖNCE gelmeli:
// tarayıcının preflight (OPTIONS) isteğine, yetki kontrolüne takılmadan cevap verilebilmesi için.
// Yanlış sırada olursa preflight 401/405 alır ve tarayıcı asıl isteği HİÇ göndermez.
app.UseCors(FrontendCorsPolicy);

// Sıra KRİTİK: önce "kimsin?" (Authentication), sonra "yetkin var mı?" (Authorization).
// Ters olsaydı Authorization çalışırken kullanıcı henüz tanınmamış olur, her istek 401 alırdı.
app.UseAuthentication();     // Bearer token'ı doğrula → HttpContext.User
app.UseAuthorization();      // Fallback policy / [Authorize] / [AllowAnonymous] kontrolü
app.UseRateLimiter();        // [EnableRateLimiting] olan endpoint'lerde istek sayısını sınırla

app.MapControllers();        // URL'yi doğru controller action'ına eşler (routing)

app.Run();
