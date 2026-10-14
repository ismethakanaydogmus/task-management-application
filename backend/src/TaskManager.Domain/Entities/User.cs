namespace TaskManager.Domain.Entities;

public class User
{
    public int Id { get; set; }

    // Her zaman küçük harfe çevrilmiş (normalize) halde saklanır: "Ali@Ornek.com" ile "ali@ornek.com"
    // iki ayrı hesap olmasın. Veritabanında UNIQUE index var (yarış durumunda bile çift kayıt olamaz).
    public required string Email { get; set; }

    // Şifrenin KENDİSİ DEĞİL, tek yönlü hash'i (PBKDF2 + salt). Veritabanı sızsa bile şifre geri elde edilemez.
    public required string PasswordHash { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<RefreshToken> RefreshTokens { get; set; } = [];
}
