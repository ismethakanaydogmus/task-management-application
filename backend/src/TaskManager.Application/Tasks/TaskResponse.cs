using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tasks;

// YANIT DTO'su: API'nin dışarıya GÖSTERDİĞİ şekil.
//
// Neden entity'yi (TaskItem) doğrudan döndürmüyoruz?
//  1) Güvenlik: entity'ye ileride eklenecek iç alanlar (UserId, IsDeleted, PasswordHash...)
//     otomatik olarak JSON'a girip dışarı sızardı. DTO'da sadece açıkça yazdıklarımız var.
//  2) Bağımsızlık: veritabanı yapısı değişse bile API sözleşmesi (frontend'in gördüğü) değişmez.
//  3) Döngüsel referans: ilişkiler eklenince (Task.Project.Tasks...) entity'yi serileştirmek
//     sonsuz döngü / devasa JSON üretebilir.
public record TaskResponse(
    int Id,
    string Title,
    string? Description,
    bool IsCompleted,
    TaskPriority Priority,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? CompletedAt);
