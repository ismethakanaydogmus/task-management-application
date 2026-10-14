using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        // SHA-256 → Base64 = 44 karakter
        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(64);
        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(64);

        // Her refresh isteğinde hash ile arıyoruz → index şart. Unique: aynı token iki kez kaydedilemez.
        builder.HasIndex(t => t.TokenHash).IsUnique();

        // "Kullanıcının tüm token'larını iptal et" sorgusu için
        builder.HasIndex(t => t.UserId);
    }
}
