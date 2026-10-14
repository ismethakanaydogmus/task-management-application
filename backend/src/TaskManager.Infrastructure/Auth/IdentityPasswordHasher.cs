using Microsoft.AspNetCore.Identity;
using TaskManager.Domain.Entities;
using IAppPasswordHasher = TaskManager.Application.Abstractions.IPasswordHasher;

namespace TaskManager.Infrastructure.Auth;

// ASP.NET Core Identity'nin test edilmiş PasswordHasher'ını kullanıyoruz (Identity'nin geri kalanını değil).
// Varsayılan: PBKDF2 + HMAC-SHA512, 100.000 iterasyon, kullanıcı başına rastgele 128-bit salt.
// Üretilen hash metni algoritmayı, iterasyon sayısını ve salt'ı İÇİNDE taşır; ayrı bir "salt" kolonu gerekmez.
//
// KENDİ algoritmanı YAZMA: MD5/SHA256 gibi hızlı hash'ler saniyede milyarlarca deneme yapılmasına izin verir;
// salt'sız hash'ler hazır tablolarla (rainbow table) kırılır. Bu hataları yapmamak için kanıtlanmış kütüphane.
// Alternatifler: bcrypt, Argon2id (OWASP'ın güncel ilk tercihi; .NET'te üçüncü parti paketle).
internal sealed class IdentityPasswordHasher : IAppPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string passwordHash, string password)
    {
        var result = _hasher.VerifyHashedPassword(null!, passwordHash, password);
        // SuccessRehashNeeded: şifre doğru ama hash eski parametrelerle üretilmiş (ör. eski iterasyon sayısı).
        // Gerçek bir sistemde burada hash yenilenip kaydedilir. Şimdilik "doğru" sayıyoruz.
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
