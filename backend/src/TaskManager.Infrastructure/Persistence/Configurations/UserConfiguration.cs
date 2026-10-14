using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        // UNIQUE index: aynı e-postayla iki hesap olamaz. Uygulamadaki "EmailExists" kontrolü yarış durumunda
        // (iki eşzamanlı kayıt) atlatılabilir; veritabanı kısıtı ise ASLA atlatılamaz. Son savunma hattı burası.
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        // Kullanıcı silinirse refresh token'ları da silinsin (ON DELETE CASCADE).
        builder.HasMany(u => u.RefreshTokens)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
