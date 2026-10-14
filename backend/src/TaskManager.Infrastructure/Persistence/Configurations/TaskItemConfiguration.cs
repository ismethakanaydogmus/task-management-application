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
        // Eksisi: biraz daha fazla yer kaplar. Bu ölçekte önemsiz.
        builder.Property(t => t.Priority)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Sık filtreleyeceğimiz alanlara index: "tamamlanmamış görevler" sorgusu tüm tabloyu taramasın.
        builder.HasIndex(t => t.IsCompleted);
        builder.HasIndex(t => t.DueDate);
    }
}
