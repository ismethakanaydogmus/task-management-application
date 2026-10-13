using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Models;

namespace TaskManager.Api.Data;

// DbContext = veritabanıyla bir "oturum".
//  - Sorguları SQL'e çevirir ve çalıştırır.
//  - Okuduğu nesneleri takip eder (change tracking): bir property'yi değiştirince
//    SaveChangesAsync() hangi alanların değiştiğini bilir ve sadece onlar için UPDATE üretir.
//  - SaveChangesAsync() tüm değişiklikleri TEK bir transaction içinde yazar (Unit of Work).
//
// Thread-safe DEĞİLDİR → DI'da Scoped kaydedilir (her HTTP isteğine bir tane).
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Her DbSet bir tabloyu temsil eder. LINQ sorguları buradan başlar: db.Tasks.Where(...)
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Bu assembly'deki tüm IEntityTypeConfiguration<T> sınıflarını bulup uygular.
        // Böylece her entity'nin ayarı kendi dosyasında durur; bu metot şişmez.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
