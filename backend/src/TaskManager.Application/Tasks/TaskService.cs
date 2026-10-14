using FluentValidation;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tasks;

// SERVICE katmanı = iş kurallarının yaşadığı yer.
// HTTP'den habersiz: HttpContext, IActionResult, status code... hiçbirini bilmez.
//
// Validasyon NEDEN controller'da değil de burada çalışıyor?
// Service'i kim çağırırsa çağırsın (controller, arka plan işi, başka bir service, test)
// geçersiz veri içeri GİREMEZ. Controller'da olsaydı, başka bir çağıran validasyonu atlayabilirdi.
//
// TimeProvider: DateTime.UtcNow yerine enjekte edilen saat → testte zamanı kontrol edebilmek için.
internal sealed class TaskService(
    ITaskRepository repository,
    TimeProvider clock,
    IValidator<CreateTaskRequest> createValidator,
    IValidator<UpdateTaskRequest> updateValidator) : ITaskService
{
    public async Task<IReadOnlyList<TaskResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var tasks = await repository.GetAllAsync(ct);
        return tasks.Select(t => t.ToResponse()).ToList();
    }

    public async Task<TaskResponse> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var task = await GetTaskOrThrowAsync(id, ct);
        return task.ToResponse();
    }

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct = default)
    {
        // Geçersizse FluentValidation.ValidationException fırlatır (tüm hatalar içinde) → 400
        await createValidator.ValidateAndThrowAsync(request, ct);

        var task = new TaskItem
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Priority = request.Priority,
            DueDate = request.DueDate?.ToUniversalTime(), // "+03:00" ile geldiyse UTC'ye çevir
            CreatedAt = clock.GetUtcNow().UtcDateTime
        };

        await repository.AddAsync(task, ct);
        return task.ToResponse();
    }

    public async Task UpdateAsync(int id, UpdateTaskRequest request, CancellationToken ct = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, ct);

        var task = await GetTaskOrThrowAsync(id, ct);
        var now = clock.GetUtcNow().UtcDateTime;

        // İŞ KURALI: tamamlanma zamanı, durum değiştiği AN kaydedilir.
        //  - açık → tamamlandı : CompletedAt = şimdi
        //  - tamamlandı → açık : CompletedAt temizlenir
        //  - durum değişmediyse: CompletedAt'e dokunulmaz (her PUT'ta tarih ilerlemesin)
        if (request.IsCompleted && !task.IsCompleted)
            task.CompletedAt = now;
        else if (!request.IsCompleted && task.IsCompleted)
            task.CompletedAt = null;

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();
        task.IsCompleted = request.IsCompleted;
        task.Priority = request.Priority;
        task.DueDate = request.DueDate?.ToUniversalTime();
        task.UpdatedAt = now;

        await repository.UpdateAsync(task, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var deleted = await repository.DeleteAsync(id, ct);
        if (!deleted)
            throw new NotFoundException("Görev", id);
    }

    private async Task<TaskItem> GetTaskOrThrowAsync(int id, CancellationToken ct)
        => await repository.GetByIdAsync(id, ct) ?? throw new NotFoundException("Görev", id);
}
