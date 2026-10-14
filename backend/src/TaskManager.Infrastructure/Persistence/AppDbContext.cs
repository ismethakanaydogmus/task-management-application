using Microsoft.EntityFrameworkCore;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence;

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
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Bu assembly'deki tüm IEntityTypeConfiguration<T> sınıflarını bulup uygular.
        // Böylece her entity'nin ayarı kendi dosyasında durur; bu metot şişmez.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
