using TaskManager.Application.Tasks;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Abstractions;

// DEPENDENCY INVERSION: Interface'in SAHİBİ Application; gerçekleştirimi (EfTaskRepository) Infrastructure'da.
//
// AŞAMA 8 — SAHİPLİK: Tek kayıt getiren/silen metotlar userId de alıyor.
// Sorgu "WHERE id = @id AND user_id = @userId" olur → başkasının görevi için sonuç "bulunamadı" (null/false).
// Sahiplik kontrolü ayrı bir "if (task.UserId != me)" adımı değil, SORGUNUN KENDİSİ:
// unutulması zor, ve başkasının verisi belleğe bile gelmez.
public interface ITaskRepository
{
    Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> GetPagedAsync(int userId, TaskQuery query, CancellationToken ct = default);
    Task<TaskSummary> GetSummaryAsync(int userId, CancellationToken ct = default);
    Task<TaskItem?> GetByIdAsync(int id, int userId, CancellationToken ct = default);
    Task AddAsync(TaskItem task, CancellationToken ct = default);
    Task UpdateAsync(TaskItem task, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, int userId, CancellationToken ct = default);
}
