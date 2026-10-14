using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Api.ErrorHandling;

// Pipeline'ın en başındaki UseExceptionHandler() middleware'i, aşağıda (controller, service, repository)
// fırlatılan ve yakalanmayan HER exception'ı buraya getirir.
//
// Görevi: exception'ı doğru HTTP durum koduna ve TEK TİP bir ProblemDetails JSON'una çevirmek.
//  - Beklenen hatalar (validasyon, bulunamadı) → 4xx, kullanıcıya anlamlı mesaj, log seviyesi düşük
//  - Beklenmeyen hatalar                       → 500, kullanıcıya GENEL mesaj, log'a TÜM detay
//
// Alternatif: her controller action'ına try/catch yazmak → tekrar eden kod, bir yerde unutulur,
// her geliştirici farklı formatta hata döndürür.
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        // İstemci bağlantıyı kapattıysa (sayfayı kapattı, isteği iptal etti) bu bir HATA değil.
        // Cevap verecek kimse yok; error log'u kirletmeyelim.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("İstek istemci tarafından iptal edildi: {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
            httpContext.Response.StatusCode = 499; // "Client Closed Request" (nginx konvansiyonu)
            return true;
        }

        ProblemDetails problem = exception switch
        {
            ValidationException validation => CreateValidationProblem(validation),

            NotFoundException notFound => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Kayıt bulunamadı",
                Detail = notFound.Message
            },

            UnauthorizedException unauthorized => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Kimlik doğrulanamadı",
                Detail = unauthorized.Message
            },

            ConflictException conflict => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Çakışma",
                Detail = conflict.Message
            },

            // Beklenmeyen her şey. Detay (exception mesajı, stack trace) istemciye GİTMEZ:
            // veritabanı adı, tablo yapısı, dosya yolları gibi saldırgana ipucu verecek bilgiler içerebilir.
            // Geliştirme ortamında hata ayıklamayı kolaylaştırmak için sadece mesajı ekliyoruz.
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Beklenmeyen bir hata oluştu",
                Detail = environment.IsDevelopment() ? exception.Message : null
            }
        };

        // Log seviyesi hatanın türüne göre: 4xx = istemci hatası (Warning bile fazla olabilir), 5xx = bizim hatamız.
        if (problem.Status >= 500)
            logger.LogError(exception, "Yakalanmamış hata: {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogInformation("İstemci hatası {Status}: {Message}", problem.Status, exception.Message);

        httpContext.Response.StatusCode = problem.Status!.Value;

        // IProblemDetailsService: AddProblemDetails() ile kaydedilen ortak ayarları (traceId, instance, type linki)
        // uygulayarak yazar. Böylece burada üretilen hatalar ile framework'ün ürettiği 404/400'ler AYNI formatta olur.
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    // FluentValidation hatalarını { "alanAdı": ["mesaj1", "mesaj2"] } sözlüğüne çevirir.
    // Frontend bu sayede her mesajı ilgili form alanının altında gösterebilir.
    private static ValidationProblemDetails CreateValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName)) // "Title" → "title" (JSON'daki adıyla aynı)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bir veya daha fazla alan geçersiz"
        };
    }
}
