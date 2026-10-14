using FluentValidation;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tasks;

// GET /api/tasks?status=active&priority=High&search=market&sortBy=dueDate&sortDirection=asc&page=2&pageSize=20
// Query string'deki parametreler bu nesneye bağlanır ([FromQuery]).
public record TaskQuery(
    TaskStatusFilter Status = TaskStatusFilter.All,
    TaskPriority? Priority = null,
    string? Search = null,
    TaskSortField SortBy = TaskSortField.CreatedAt,
    SortDirection SortDirection = SortDirection.Desc,
    int Page = 1,
    int PageSize = 20);

public enum TaskStatusFilter { All, Active, Completed }

// SIRALAMA WHITELIST'i: Kullanıcı sadece bu alanlara göre sıralayabilir.
// "sortBy" metnini olduğu gibi sorguya koysaydık (ör. dinamik SQL "ORDER BY " + sortBy):
//  - SQL injection: sortBy="id; DROP TABLE tasks"
//  - İndekssiz bir kolona göre sıralatıp veritabanını yormak, iç kolonları (password_hash) keşfetmek
// Enum kullanınca geçersiz değer model binding'de 400 ile reddedilir.
public enum TaskSortField { CreatedAt, DueDate, Priority, Title }

public enum SortDirection { Asc, Desc }

// "Görevlerim" özetinin sayıları (filtre butonlarındaki rozetler). Sayfalamadan BAĞIMSIZ.
public record TaskSummary(int All, int Active, int Completed);

internal sealed class TaskQueryValidator : AbstractValidator<TaskQuery>
{
    public const int MaxPageSize = 100;

    public TaskQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Sayfa 1 veya daha büyük olmalı.");

        // Üst sınır olmasaydı: ?pageSize=1000000 ile tek istekte tüm tabloyu çekmek (DoS) mümkün olurdu.
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize).WithMessage($"Sayfa boyutu 1 ile {MaxPageSize} arasında olmalı.");

        RuleFor(x => x.Search)
            .MaximumLength(100).WithMessage("Arama metni en fazla 100 karakter olabilir.");

        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.SortBy).IsInEnum();
        RuleFor(x => x.SortDirection).IsInEnum();
    }
}
