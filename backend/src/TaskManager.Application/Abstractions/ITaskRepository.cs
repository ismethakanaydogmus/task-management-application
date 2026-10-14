using TaskManager.Domain.Entities;

namespace TaskManager.Application.Abstractions;

// DEPENDENCY INVERSION burada gerçekleşiyor:
// Interface'in SAHİBİ Application katmanı (iş mantığına ne lazımsa onu tanımlar).
// Gerçekleştirimi (EfTaskRepository) ise Infrastructure'da.
// Yani Application → Infrastructure'a değil, Infrastructure → Application'a bağımlı.
// Application, EF Core'un veya PostgreSQL'in varlığından habersiz.
//
// CancellationToken: istek iptal edilirse (kullanıcı sayfayı kapattı, timeout)
// veritabanı sorgusunun da iptal edilebilmesi için en alta kadar taşınır.
public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken ct = default);
    Task<TaskItem?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(TaskItem task, CancellationToken ct = default);
    Task UpdateAsync(TaskItem task, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
