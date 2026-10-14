using TaskManager.Application.Abstractions;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tasks;

// SERVICE katmanı = iş kurallarının yaşadığı yer.
// HTTP'den habersiz: HttpContext, IActionResult, status code... hiçbirini bilmez.
// Bu sayede aynı mantık bir controller'dan, bir arka plan işinden veya bir testten çağrılabilir.
//
// TimeProvider (.NET 8+): DateTime.UtcNow yerine "saat" de enjekte ediliyor.
// Neden? Testte "şu an 2026-01-01 10:00" diyebilmek için (Aşama 9). DateTime.UtcNow'u test edemezsin.
internal sealed class TaskService(ITaskRepository repository, TimeProvider clock) : ITaskService
{
    public async Task<IReadOnlyList<TaskResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var tasks = await repository.GetAllAsync(ct);
        return tasks.Select(t => t.ToResponse()).ToList();
    }

    public async Task<TaskResponse?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var task = await repository.GetByIdAsync(id, ct);
        return task?.ToResponse();
    }

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct = default)
    {
        var task = new TaskItem
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Priority = request.Priority,
            DueDate = request.DueDate,
            CreatedAt = clock.GetUtcNow().UtcDateTime
        };

        await repository.AddAsync(task, ct);
        return task.ToResponse();
    }

    public async Task<bool> UpdateAsync(int id, UpdateTaskRequest request, CancellationToken ct = default)
    {
        var task = await repository.GetByIdAsync(id, ct);
        if (task is null)
            return false;

        var now = clock.GetUtcNow().UtcDateTime;

        // İŞ KURALI: tamamlanma zamanı, durum değiştiği AN kaydedilir.
        //  - açık → tamamlandı : CompletedAt = şimdi
        //  - tamamlandı → açık : CompletedAt temizlenir
        //  - durum değişmediyse: CompletedAt'e dokunulmaz (her PUT'ta tarih ilerlemesin)
        // Bu kural controller'da olsaydı: aynı işi yapan başka bir endpoint (ör. PATCH /complete)
        // veya arka plan işi kuralı kopyalamak zorunda kalır, biri unutulunca veri tutarsızlaşırdı.
        if (request.IsCompleted && !task.IsCompleted)
            task.CompletedAt = now;
        else if (!request.IsCompleted && task.IsCompleted)
            task.CompletedAt = null;

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();
        task.IsCompleted = request.IsCompleted;
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;
        task.UpdatedAt = now;

        await repository.UpdateAsync(task, ct);
        return true;
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        => repository.DeleteAsync(id, ct);
}
