using TaskManager.Api.Models;

namespace TaskManager.Api.Contracts;

// DTO (Data Transfer Object) = istemci ile API arasında taşınan veri şekli.
//
// Neden POST'ta doğrudan TaskItem almıyoruz?
// TaskItem'da Id, CreatedAt, UpdatedAt var. Bunları istemci belirlememeli.
// TaskItem'ı doğrudan alsaydık istemci {"id": 999, "createdAt": "2000-01-01"} gönderip
// bu alanları kendisi doldurabilirdi. Buna "over-posting" (mass assignment) açığı denir.
// DTO ile istemcinin GÖNDEREBİLECEĞİ alanları açıkça sınırlıyoruz.
//
// "record" neden? Sadece veri taşıyan, değişmesine gerek olmayan (immutable) tipler için kısa ve ideal.
// Tek satırda constructor, property ve Equals/ToString üretir.

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
