using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TaskManager.Api.ErrorHandling;

// Validasyon İKİ aşamada olur:
//  1) Model binding (JSON → C# nesnesi): bozuk JSON, "priority": "CokYuksek", yanlış tip...
//     Bu aşamadaki hatalar daha validator'ımız ÇALIŞMADAN [ApiController] tarafından 400'e çevrilir.
//  2) FluentValidation (nesne oluştu, kurallara uyuyor mu?)
//
// 1. aşamanın varsayılan çıktısı:
//   "errors": { "$.priority": ["The JSON value could not be converted to TaskManager.Application.Tasks.CreateTaskRequest..."] }
//   - Alan adı "$.priority" → frontend "priority" alanıyla eşleştiremez
//   - İngilizce, teknik mesaj ve İÇ TİP ADIMIZ dışarı sızıyor
// Bu sınıf onu 2. aşamayla aynı şekle getirir: { "priority": ["Geçersiz değer veya format."] }
internal static class InvalidModelStateResponse
{
    public static IActionResult Create(ActionContext context)
    {
        var normalized = new ModelStateDictionary();

        foreach (var (key, entry) in context.ModelState)
        {
            if (entry.Errors.Count == 0)
                continue;

            normalized.AddModelError(NormalizeKey(key), ToFriendlyMessage(key));
        }

        // ProblemDetailsFactory: AddProblemDetails'teki ayarları (traceId, instance) uygular → format tutarlı kalır.
        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = factory.CreateValidationProblemDetails(
            context.HttpContext,
            normalized,
            statusCode: StatusCodes.Status400BadRequest,
            title: "Bir veya daha fazla alan geçersiz");

        return new BadRequestObjectResult(problem);
    }

    // "$.priority" → "priority",  "$" veya action parametre adı ("request") → "body"
    private static string NormalizeKey(string key)
    {
        if (key.StartsWith("$.", StringComparison.Ordinal))
            return key[2..];

        return key is "$" or "" or "request" ? "body" : key;
    }

    private static string ToFriendlyMessage(string key) =>
        key.StartsWith("$.", StringComparison.Ordinal)
            ? "Geçersiz değer veya format."
            : "İstek gövdesi boş veya geçerli bir JSON değil.";
}
