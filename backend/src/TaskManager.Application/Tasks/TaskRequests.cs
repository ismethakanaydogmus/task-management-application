using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tasks;

// İSTEK DTO'ları: istemcinin GÖNDEREBİLECEĞİ alanlar.
// Entity'yi (TaskItem) doğrudan almıyoruz; aksi halde istemci Id, CreatedAt gibi alanları
// kendisi doldurabilirdi (over-posting / mass assignment).

public record CreateTaskRequest(
    string Title,
    string? Description,
    TaskPriority Priority = TaskPriority.Medium,
    DateTime? DueDate = null);

// PUT = kaynağın TAMAMINI değiştirir; bu yüzden tüm düzenlenebilir alanlar burada.
public record UpdateTaskRequest(
    string Title,
    string? Description,
    bool IsCompleted,
    TaskPriority Priority,
    DateTime? DueDate);
