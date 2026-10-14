using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Abstractions;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Repositories;

// ITaskRepository'nin (Application'da tanımlı) PostgreSQL / EF Core gerçekleştirimi.
// "internal": Infrastructure dışından kimse bu sınıfı doğrudan kullanamaz, sadece interface üzerinden.
//
// SCOPED kaydedilir, çünkü içindeki AppDbContext Scoped.
// (Singleton yapsaydık "captive dependency" olurdu: tek bir DbContext tüm isteklerce paylaşılırdı.)
internal sealed class EfTaskRepository(AppDbContext db) : ITaskRepository
{
    public async Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Tasks
            // AsNoTracking: sadece okuyup DTO'ya çevireceğiz, değiştirmeyeceğiz.
            // EF'nin her nesneyi takip listesine eklemesine gerek yok → daha az bellek, daha hızlı.
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct); // Sorgu ANCAK burada veritabanına gider (deferred execution)
    }

    public async Task<TaskItem?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        // FindAsync önce DbContext'in takip ettiği nesnelere bakar, yoksa veritabanına gider.
        // Burada tracking İSTİYORUZ: service bu nesneyi değiştirip UpdateAsync çağırıyor.
        return await db.Tasks.FindAsync([id], ct);
    }

    public async Task AddAsync(TaskItem task, CancellationToken ct = default)
    {
        db.Tasks.Add(task);            // sadece "eklenecek" diye işaretler, henüz SQL yok
        await db.SaveChangesAsync(ct); // INSERT burada çalışır; Postgres'in ürettiği Id task.Id'ye yazılır
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken ct = default)
    {
        // Nesne GetByIdAsync ile okunduğu için zaten takip ediliyor;
        // EF hangi property'lerin değiştiğini biliyor → SaveChanges sadece onlar için UPDATE üretir.
        //
        // Her durumda db.Tasks.Update(task) çağırsaydık: EF TÜM kolonları "değişti" sayar,
        // değişmeyen created_at dahil her alanı yazardı (Aşama 2'de log'da yakaladığımız hata).
        // Update() sadece takip EDİLMEYEN (Detached) bir nesne geldiğinde gereklidir.
        if (db.Entry(task).State == EntityState.Detached)
            db.Tasks.Update(task);

        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        // ExecuteDeleteAsync (EF 7+): önce SELECT edip sonra silmek yerine
        // doğrudan "DELETE FROM tasks WHERE id = @id" çalıştırır → tek sorgu.
        var affected = await db.Tasks
            .Where(t => t.Id == id)
            .ExecuteDeleteAsync(ct);
        return affected > 0;
    }
}
