using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
using TaskManager.Api.Models;

namespace TaskManager.Api.Repositories;

// ITaskRepository'nin PostgreSQL (EF Core) gerçekleştirimi.
// Controller bu sınıfın varlığından habersiz; sadece Program.cs'teki DI kaydı değişti.
//
// SCOPED kaydedilir, çünkü içindeki AppDbContext Scoped.
// (Singleton yapsaydık "captive dependency" olurdu: tek bir DbContext tüm isteklerce paylaşılırdı.)
public class EfTaskRepository(AppDbContext db) : ITaskRepository
{
    public async Task<IReadOnlyList<TaskItem>> GetAllAsync()
    {
        return await db.Tasks
            // AsNoTracking: sadece okuyup JSON'a çevireceğiz, değiştirmeyeceğiz.
            // EF'nin her nesneyi takip listesine eklemesine gerek yok → daha az bellek, daha hızlı.
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(); // Sorgu ANCAK burada veritabanına gider (deferred execution)
    }

    public async Task<TaskItem?> GetByIdAsync(int id)
    {
        // FindAsync önce DbContext'in takip ettiği nesnelere bakar, yoksa veritabanına gider.
        // Burada tracking İSTİYORUZ: controller bu nesneyi değiştirip UpdateAsync çağırıyor.
        return await db.Tasks.FindAsync(id);
    }

    public async Task<TaskItem> AddAsync(TaskItem task)
    {
        db.Tasks.Add(task);          // sadece "eklenecek" diye işaretler, henüz SQL yok
        await db.SaveChangesAsync(); // INSERT burada çalışır; Postgres'in ürettiği Id task.Id'ye yazılır
        return task;
    }

    public async Task<bool> UpdateAsync(TaskItem task)
    {
        // Nesne GetByIdAsync ile okunduğu için zaten takip ediliyor (state = Unchanged/Modified);
        // EF hangi property'lerin değiştiğini biliyor → SaveChanges sadece onlar için UPDATE üretir.
        //
        // Her durumda db.Tasks.Update(task) çağırsaydık: EF TÜM kolonları "değişti" sayar ve
        // "UPDATE tasks SET created_at=..., description=..., title=..." gibi tüm alanları yazardı.
        // Hem gereksiz yük, hem de eşzamanlı başka bir isteğin değiştirdiği alanı sessizce ezebilir.
        // Update() sadece takip EDİLMEYEN (Detached) bir nesne geldiğinde gereklidir.
        if (db.Entry(task).State == EntityState.Detached)
            db.Tasks.Update(task);

        var affected = await db.SaveChangesAsync();
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        // ExecuteDeleteAsync (EF 7+): önce SELECT edip sonra silmek yerine
        // doğrudan "DELETE FROM tasks WHERE id = @id" çalıştırır → tek sorgu.
        var affected = await db.Tasks
            .Where(t => t.Id == id)
            .ExecuteDeleteAsync();
        return affected > 0;
    }
}
