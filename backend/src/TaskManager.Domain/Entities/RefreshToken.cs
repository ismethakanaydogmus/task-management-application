namespace TaskManager.Domain.Entities;

// Uzun ömürlü oturum anahtarı. Access token'ın (JWT) aksine VERİTABANINDA tutulur, bu yüzden İPTAL EDİLEBİLİR.
//
// Token'ın KENDİSİNİ değil, SHA-256 hash'ini saklıyoruz.
// Veritabanı sızarsa saldırgan hash'ten token'ı geri elde edip oturumları ele geçiremez.
// (Şifrelerden farklı olarak burada HIZLI bir hash yeterli: token 64 baytlık rastgele bir değer,
//  tahmin edilebilir bir kelime değil; kaba kuvvetle bulunması imkânsız.)
public class RefreshToken
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public required string TokenHash { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    // Kullanıldığında (rotation) veya çıkış yapıldığında doldurulur.
    public DateTime? RevokedAt { get; set; }

    // ROTATION: her refresh'te bu token iptal edilip yerine yenisi verilir; yenisinin hash'i buraya yazılır.
    // İptal edilmiş bir token TEKRAR kullanılmaya çalışılırsa bu "çalınmış token" işaretidir (reuse detection).
    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive(DateTime now) => RevokedAt is null && ExpiresAt > now;
}
