using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Configurations;

// "Fluent API" ile tablo ayarları.
// Alternatif: TaskItem sınıfının üstüne [MaxLength(200)], [Required] gibi attribute'lar koymak (Data Annotations).
// Fluent API'yi seçtik çünkü:
//  - Model sınıfı veritabanı detaylarından temiz kalır.
//  - Attribute'larla yapılamayan ayarlar (index, enum dönüşümü, composite key) sadece burada yapılabilir.
//  - Tüm DB ayarları tek bir yerde durur.
public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("tasks");

        builder.HasKey(t => t.Id); // konvansiyonla zaten "Id" anahtardır; açıkça yazmak okunurluk için

        // Sınır koymasaydık Postgres'te "text" (sınırsız) olurdu. Biri 50 MB'lık bir başlık gönderebilirdi.
        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(2000);

        // Enum'ı veritabanında sayı (2) yerine metin ("High") olarak sakla.
        // Artısı: SQL ile bakınca okunur; enum sırası değişirse eski veriler bozulmaz.
        // Eksisi: biraz daha fazla yer kaplar ve SIRALAMA alfabetik olur ("High" < "Low" < "Medium")!
        // Aşama 8'de önceliğe göre sıralarken bunu CASE ifadesiyle çözüyoruz (EfTaskRepository).
        builder.Property(t => t.Priority)
            .HasConversion<string>()
            .HasMaxLength(20);

        // SAHİPLİK: Her görev bir kullanıcıya ait. Kullanıcı silinirse görevleri de silinir (CASCADE).
        // Navigation property (TaskItem.User) eklemedik; sadece foreign key yeterli.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // INDEX'LER (Aşama 8'de yeniden düzenlendi):
        // Artık HER sorgu "WHERE user_id = @me" ile başlıyor. Bu yüzden index'ler user_id ile BAŞLAMALI.
        //
        // (user_id, created_at): varsayılan liste ("benim görevlerim, en yeniden eskiye") + sayfalama.
        //   Composite index'te SIRA önemli: önce user_id ile daraltır, sonra created_at sırasıyla okur;
        //   ayrıca sıralama (ORDER BY) yapmasına gerek kalmaz.
        // (user_id, due_date): "son tarihe göre sırala".
        //
        // Aşama 2'deki tek başına is_completed index'i KALDIRILDI: boolean kolonun sadece 2 değeri var
        // (düşük seçicilik); veritabanı zaten çoğu zaman bu index'i kullanmaz, ama her INSERT/UPDATE'te güncellemek zorunda kalır.
        builder.HasIndex(t => new { t.UserId, t.CreatedAt });
        builder.HasIndex(t => new { t.UserId, t.DueDate });
    }
}
