using FluentValidation;

namespace TaskManager.Application.Common.Validation;

// Birden fazla validator'da tekrar eden kurallar için extension metotlar.
public static class ValidationRules
{
    // Aşama 2'deki "bilinen sorun"un çözümü.
    // "2026-10-20T18:00:00" (saat dilimi YOK)   → Kind = Unspecified → REDDET (hangi an olduğu belirsiz)
    // "2026-10-20T18:00:00Z"                     → Kind = Utc         → kabul
    // "2026-10-20T18:00:00+03:00"                → Kind = Local       → kabul (service UTC'ye çevirir)
    //
    // Bu kontrol olmasaydı Npgsql veritabanına yazarken exception fırlatır, istemci 500 alırdı.
    // Oysa hata istemcinin; doğru cevap 400 ve ne yapması gerektiğini söyleyen bir mesaj.
    public static IRuleBuilderOptions<T, DateTime?> MustHaveTimeZone<T>(this IRuleBuilder<T, DateTime?> rule) =>
        rule.Must(date => date is null || date.Value.Kind != DateTimeKind.Unspecified)
            .WithMessage("Tarih saat dilimi içermeli. Örnek: 2026-10-20T18:00:00Z veya 2026-10-20T18:00:00+03:00");
}
