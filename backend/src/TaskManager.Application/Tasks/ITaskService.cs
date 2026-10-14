namespace TaskManager.Application.Tasks;

// Controller'ın konuştuğu sözleşme. Dışarıya sadece DTO'lar çıkar; entity'ler Application içinde kalır.
//
// "Bulunamadı" durumunu nasıl bildiriyoruz?
//  - GetById → null,  Update/Delete → false.
// Basit ve açık. Alternatifler (Aşama 4'te tartışacağız):
//  - Exception fırlatmak (NotFoundException) → global handler 404'e çevirir
//  - Result pattern → Result<TaskResponse>.NotFound()
public interface ITaskService
{
    Task<IReadOnlyList<TaskResponse>> GetAllAsync(CancellationToken ct = default);
    Task<TaskResponse?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, UpdateTaskRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
