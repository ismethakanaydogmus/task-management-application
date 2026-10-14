using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;

namespace TaskManager.Api.Hosting;

// TEK CONTAINER DAĞITIMI (Azure Container Apps): API, React build çıktısını (wwwroot) KENDİSİ sunar.
//
// Neden frontend'i ayrı bir yerde (ör. Azure Static Web Apps) yayınlamadık?
// Frontend ve API farklı Azure alan adlarında olsaydı (xxx.azurestaticapps.net / yyy.azurecontainerapps.io)
// tarayıcı için FARKLI SİTE olurlardı:
//  - Refresh token cookie'si SameSite=Strict → cross-site istekte GÖNDERİLMEZ → sayfa yenilenince oturum düşer.
//  - Tarayıcılar üçüncü taraf cookie'leri giderek daha fazla engelliyor.
//  - CORS + preflight geri gelir.
// Tek origin, Aşama 10'daki nginx kurulumunun yaptığını yapar; bu sefer nginx'in işini ASP.NET üstleniyor.
//
// docker-compose.prod.yml'deki nginx kurulumu da çalışmaya devam eder: orada "final" image'ında wwwroot yok,
// bu yüzden bu sınıf hiçbir şey eklemez (aşağıdaki IsEnabled kontrolü).
public static class SpaHosting
{
    // nginx.conf'taki ile AYNI politika. XSS ile sayfaya sokulan script/stil ÇALIŞMAZ.
    private const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; " +
        "font-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";

    public static bool IsEnabled(IWebHostEnvironment env) =>
        env.WebRootPath is not null && File.Exists(Path.Combine(env.WebRootPath, "index.html"));

    // Middleware: statik dosyalar + güvenlik başlıkları. Authentication/Authorization'dan ÖNCE çağrılır:
    // JS/CSS dosyaları giriş gerektirmez (giriş ekranının kendisi bu dosyalarla çiziliyor).
    public static void UseSpaStaticFiles(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers[HeaderNames.ContentSecurityPolicy] = ContentSecurityPolicy;
            headers[HeaderNames.XContentTypeOptions] = "nosniff";
            headers[HeaderNames.XFrameOptions] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            // HTTPS zorunlu (Azure ingress TLS'i sağlıyor): tarayıcı bu siteye 1 yıl boyunca SADECE HTTPS ile gelsin.
            if (context.Request.IsHttps)
                headers[HeaderNames.StrictTransportSecurity] = "max-age=31536000; includeSubDomains";
            await next();
        });

        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                // Vite dosya adlarına içerik hash'i ekler (index-hmbIFdCm.js) → içerik değişirse AD değişir → 1 yıl önbellek.
                if (ctx.Context.Request.Path.StartsWithSegments("/assets"))
                    ctx.Context.Response.Headers[HeaderNames.CacheControl] = "public, max-age=31536000, immutable";
            }
        });
    }

    // Endpoint'ler: SPA fallback + /api altındaki bilinmeyen yollar için 404.
    public static void MapSpaFallback(this WebApplication app)
    {
        // Bilinmeyen bir /api/... adresi index.html DÖNMEMELİ (istemci JSON bekliyor) → 404 ProblemDetails.
        // Fallback'ten daha spesifik olduğu için öncelikli eşleşir.
        app.Map("/api/{**rest}", () => Results.NotFound()).AllowAnonymous();

        // SPA FALLBACK: /gorevler/5 gibi dosya olmayan bir adres doğrudan açılınca index.html dön; yönlendirmeyi React yapar.
        // index.html ASLA önbelleğe alınmaz: yeni sürümde tarayıcı yeni JS dosya adlarını görsün (aksi halde beyaz ekran).
        app.MapFallbackToFile("index.html", new StaticFileOptions
        {
            OnPrepareResponse = ctx => ctx.Context.Response.Headers[HeaderNames.CacheControl] = "no-cache"
        }).AllowAnonymous(); // fallback policy (giriş zorunlu) giriş ekranını engellemesin
    }
}
