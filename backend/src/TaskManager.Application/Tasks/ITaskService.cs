using TaskManager.Application.Common;

namespace TaskManager.Application.Tasks;

// Controller'ın konuştuğu sözleşme. Dışarıya sadece DTO'lar çıkar; entity'ler Application içinde kalır.
//
// Hata durumları EXCEPTION ile bildirilir:
//  - Kayıt yok VEYA başkasına ait → NotFoundException   → 404 (başkasının görevinin VAR OLDUĞUNU bile sızdırmayız)
//  - Girdi geçersiz               → ValidationException → 400
//
// Hiçbir metot userId ALMIYOR: kimlik ICurrentUser'dan gelir (bkz. ICurrentUser.cs).
public interface ITaskService
{
    Task<PagedResult<TaskResponse>> GetPagedAsync(TaskQuery query, CancellationToken ct = default);
    Task<TaskSummary> GetSummaryAsync(CancellationToken ct = default);
    Task<TaskResponse> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct = default);
    Task UpdateAsync(int id, UpdateTaskRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
