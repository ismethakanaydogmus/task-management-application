using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tasks;

// Entity → DTO dönüşümü ELLE (extension method).
//
// Alternatif: AutoMapper / Mapster → mapper.Map<TaskResponse>(task)
//  + Daha az kod
//  - Eşleştirme "sihirli": bir property adı değişince hata DERLEME değil ÇALIŞMA anında çıkar
//  - "Bu alan nereden geliyor?" diye ararken F12 (Go to Definition) işe yaramaz
//  - AutoMapper artık ticari lisanslı
// Elle mapping: açık, hızlı, eksik alan constructor'da derleme hatası verir.
public static class TaskMappings
{
    public static TaskResponse ToResponse(this TaskItem task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.IsCompleted,
        task.Priority,
        task.DueDate,
        task.CreatedAt,
        task.UpdatedAt,
        task.CompletedAt);
}
