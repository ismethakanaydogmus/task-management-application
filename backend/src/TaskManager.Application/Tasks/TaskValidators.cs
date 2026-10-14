using FluentValidation;
using TaskManager.Application.Common.Validation;

namespace TaskManager.Application.Tasks;

// GİRDİ VALİDASYONU: "İstek düzgün mü?" Veritabanına bakmadan cevaplanabilen kurallar.
// İş kuralları ("tamamlanmış görev silinemez" gibi) burada değil, TaskService'te.
//
// Sınırlar TaskItemConfiguration'daki kolon uzunluklarıyla AYNI olmalı (200 / 2000).
// Validator olmasaydı 300 karakterlik başlık veritabanına kadar gider, Postgres
// "value too long for type character varying(200)" hatası verir, istemci 500 alırdı.

internal sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        // NotEmpty: null, "" ve sadece boşluktan oluşan ("   ") metinleri reddeder.
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Başlık boş olamaz.")
            .MaximumLength(200).WithMessage("Başlık en fazla 200 karakter olabilir.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Açıklama en fazla 2000 karakter olabilir.");

        // Neden gerekli? JsonStringEnumConverter metnin yanında SAYI da kabul eder:
        // {"priority": 99} → (TaskPriority)99 → tanımsız bir enum değeri veritabanına "99" olarak yazılırdı.
        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Öncelik Low, Medium veya High olmalı.");

        RuleFor(x => x.DueDate).MustHaveTimeZone();
    }
}

internal sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Başlık boş olamaz.")
            .MaximumLength(200).WithMessage("Başlık en fazla 200 karakter olabilir.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Açıklama en fazla 2000 karakter olabilir.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Öncelik Low, Medium veya High olmalı.");

        RuleFor(x => x.DueDate).MustHaveTimeZone();
    }
}
