using FluentValidation;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Common;
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
// ICurrentUser: "Bu isteği kim yapıyor?" — her okuma/yazma bu kullanıcıyla sınırlı (Aşama 8).
internal sealed class TaskService(
    ITaskRepository repository,
    ICurrentUser currentUser,
    TimeProvider clock,
    IValidator<CreateTaskRequest> createValidator,
    IValidator<UpdateTaskRequest> updateValidator,
    IValidator<TaskQuery> queryValidator) : ITaskService
{
    public async Task<PagedResult<TaskResponse>> GetPagedAsync(TaskQuery query, CancellationToken ct = default)
    {
        await queryValidator.ValidateAndThrowAsync(query, ct);

        var (items, totalCount) = await repository.GetPagedAsync(currentUser.UserId, query, ct);
        return new PagedResult<TaskResponse>(
            items.Select(t => t.ToResponse()).ToList(),
            query.Page,
            query.PageSize,
            totalCount);
    }

    public Task<TaskSummary> GetSummaryAsync(CancellationToken ct = default)
        => repository.GetSummaryAsync(currentUser.UserId, ct);

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
            // Sahip HER ZAMAN token'daki kullanıcı. İstek gövdesinde "userId" alanı YOK (DTO'da tanımlı değil),
            // yani istemci başkası adına görev oluşturamaz (over-posting koruması, Aşama 1).
            UserId = currentUser.UserId,
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
        var deleted = await repository.DeleteAsync(id, currentUser.UserId, ct);
        if (!deleted)
            throw new NotFoundException("Görev", id);
    }

    // Görev yoksa DA, başkasına aitse DE aynı NotFoundException → 404.
    // 403 ("var ama senin değil") dönseydik saldırgan id'leri deneyerek hangi görevlerin VAR olduğunu öğrenirdi.
    private async Task<TaskItem> GetTaskOrThrowAsync(int id, CancellationToken ct)
        => await repository.GetByIdAsync(id, currentUser.UserId, ct) ?? throw new NotFoundException("Görev", id);
}
