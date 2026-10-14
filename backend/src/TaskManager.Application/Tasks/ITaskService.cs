namespace TaskManager.Application.Tasks;

// Controller'ın konuştuğu sözleşme. Dışarıya sadece DTO'lar çıkar; entity'ler Application içinde kalır.
//
// Hata durumları dönüş tipiyle değil EXCEPTION ile bildirilir:
//  - Kayıt yok          → NotFoundException     → (GlobalExceptionHandler) 404
//  - Girdi geçersiz     → ValidationException   → (GlobalExceptionHandler) 400
// Aşama 3'te null/bool dönüyorduk; durum sayısı artınca bool yetmiyordu.
public interface ITaskService
{
    Task<IReadOnlyList<TaskResponse>> GetAllAsync(CancellationToken ct = default);
    Task<TaskResponse> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct = default);
    Task UpdateAsync(int id, UpdateTaskRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
