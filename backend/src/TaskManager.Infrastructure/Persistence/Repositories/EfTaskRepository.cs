using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Tasks;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Repositories;

// ITaskRepository'nin (Application'da tanımlı) PostgreSQL / EF Core gerçekleştirimi.
// SCOPED kaydedilir, çünkü içindeki AppDbContext Scoped.
internal sealed class EfTaskRepository(AppDbContext db) : ITaskRepository
{
    // Tüm sorguların başladığı yer: SADECE bu kullanıcının görevleri.
    // Sahiplik filtresini tek bir yerde toplamak, bir sorguda unutma ihtimalini azaltır.
    private IQueryable<TaskItem> TasksOf(int userId) => db.Tasks.Where(t => t.UserId == userId);

    public async Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> GetPagedAsync(
        int userId, TaskQuery query, CancellationToken ct = default)
    {
        // IQueryable üzerinde zincirleme: HİÇBİR satır henüz SQL'e gitmedi (deferred execution).
        // Tüm filtreler tek bir SQL'in WHERE'ine dönüşür; filtreleme veritabanında, index'lerle yapılır.
        var tasks = TasksOf(userId).AsNoTracking();

        tasks = query.Status switch
        {
            TaskStatusFilter.Active => tasks.Where(t => !t.IsCompleted),
            TaskStatusFilter.Completed => tasks.Where(t => t.IsCompleted),
            _ => tasks
        };

        if (query.Priority is { } priority)
            tasks = tasks.Where(t => t.Priority == priority);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // ILIKE: büyük/küçük harf duyarsız LIKE (PostgreSQL'e özgü → bu kod Infrastructure'da).
            //
            // LIKE'ta "%" (her şey) ve "_" (tek karakter) JOKER karakterlerdir. Kullanıcı "%" ararsa
            // escape etmeseydik TÜM görevler eşleşirdi. SQL injection değil (EF parametre kullanır),
            // ama "LIKE injection": kullanıcı girdisi kalıbın anlamını değiştirir. Kaçış karakteriyle düz metne çeviriyoruz.
            var pattern = $"%{EscapeLike(query.Search.Trim())}%";
            tasks = tasks.Where(t =>
                EF.Functions.ILike(t.Title, pattern, @"\") ||
                (t.Description != null && EF.Functions.ILike(t.Description, pattern, @"\")));
            // Not: "%metin%" kalıbı normal (B-tree) index kullanamaz → büyük tablolarda yavaş.
            // Çözüm: pg_trgm uzantısı + GIN index, veya PostgreSQL full-text search (tsvector).
        }

        // TOPLAM SAYI: Filtreler uygulandıktan SONRA, sayfalamadan ÖNCE. ("12 sonuçtan 1–5 arası")
        // Ayrı bir COUNT(*) sorgusu. Çok büyük tablolarda pahalıdır; o yüzden bazı sistemler toplam sayı göstermez.
        var totalCount = await tasks.CountAsync(ct);

        var items = await ApplySorting(tasks, query)
            // OFFSET SAYFALAMA: (sayfa-1) * boyut kadar satırı ATLA, sonra boyut kadar AL.
            // SQL: ... LIMIT @pageSize OFFSET @skip
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    private static IQueryable<TaskItem> ApplySorting(IQueryable<TaskItem> tasks, TaskQuery query)
    {
        var asc = query.SortDirection == SortDirection.Asc;

        IOrderedQueryable<TaskItem> ordered = query.SortBy switch
        {
            // Öncelik veritabanında METİN ("High"/"Low"/"Medium") → doğrudan sıralasak ALFABETİK olurdu:
            // High, Low, Medium (yanlış!). CASE ifadesiyle anlamlı sıraya çeviriyoruz:
            //   SQL: ORDER BY CASE WHEN priority = 'High' THEN 2 WHEN priority = 'Medium' THEN 1 ELSE 0 END
            TaskSortField.Priority => asc
                ? tasks.OrderBy(t => t.Priority == TaskPriority.High ? 2 : t.Priority == TaskPriority.Medium ? 1 : 0)
                : tasks.OrderByDescending(t => t.Priority == TaskPriority.High ? 2 : t.Priority == TaskPriority.Medium ? 1 : 0),

            // Son tarihi OLMAYAN görevler her iki yönde de EN SONDA.
            // (PostgreSQL varsayılanı: ASC'de NULL'lar sonda, DESC'te BAŞTA — "son tarihi en geç olan" derken
            //  tarihsiz görevlerin en üstte çıkması anlamsız olurdu.)
            TaskSortField.DueDate => asc
                ? tasks.OrderBy(t => t.DueDate == null).ThenBy(t => t.DueDate)
                : tasks.OrderBy(t => t.DueDate == null).ThenByDescending(t => t.DueDate),

            TaskSortField.Title => asc ? tasks.OrderBy(t => t.Title) : tasks.OrderByDescending(t => t.Title),

            _ => asc ? tasks.OrderBy(t => t.CreatedAt) : tasks.OrderByDescending(t => t.CreatedAt),
        };

        // DETERMİNİSTİK SIRALAMA: Aynı önceliğe sahip 30 görev varsa veritabanı onları HER SORGUDA FARKLI sırada
        // döndürebilir. Sayfalama ile birleşince bir görev hem 1. hem 2. sayfada görünür, bir başkası hiç görünmez.
        // Benzersiz bir alanla (Id) son bir sıralama eklemek sırayı sabitler.
        return asc ? ordered.ThenBy(t => t.Id) : ordered.ThenByDescending(t => t.Id);
    }

    public async Task<TaskSummary> GetSummaryAsync(int userId, CancellationToken ct = default)
    {
        // Tek sorguda iki sayı: SELECT count(*), count(*) FILTER (WHERE is_completed) ... (EF uygun SQL'i üretir)
        var counts = await TasksOf(userId)
            .GroupBy(_ => 1)
            .Select(g => new { All = g.Count(), Completed = g.Count(t => t.IsCompleted) })
            .FirstOrDefaultAsync(ct);

        return counts is null
            ? new TaskSummary(0, 0, 0)
            : new TaskSummary(counts.All, counts.All - counts.Completed, counts.Completed);
    }

    public async Task<TaskItem?> GetByIdAsync(int id, int userId, CancellationToken ct = default)
    {
        // WHERE id = @id AND user_id = @userId
        // (Aşama 2-7'de FindAsync kullanıyorduk; FindAsync SADECE primary key ile arar, sahiplik koşulu eklenemez.)
        return await TasksOf(userId).FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task AddAsync(TaskItem task, CancellationToken ct = default)
    {
        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken ct = default)
    {
        // Nesne GetByIdAsync ile (sahiplik kontrollü) okunduğu için takip ediliyor; sadece değişen kolonlar yazılır.
        if (db.Entry(task).State == EntityState.Detached)
            db.Tasks.Update(task);

        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(int id, int userId, CancellationToken ct = default)
    {
        // DELETE FROM tasks WHERE id = @id AND user_id = @userId
        // Başkasının görevi → 0 satır etkilenir → false → 404.
        var affected = await TasksOf(userId)
            .Where(t => t.Id == id)
            .ExecuteDeleteAsync(ct);
        return affected > 0;
    }

    private static string EscapeLike(string value) =>
        value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
}
