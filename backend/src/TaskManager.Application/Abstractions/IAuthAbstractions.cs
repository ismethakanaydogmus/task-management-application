using TaskManager.Domain.Entities;

namespace TaskManager.Application.Abstractions;

// Application'ın kimlik doğrulama için İHTİYAÇ duyduğu yetenekler.
// Hepsinin gerçekleştirimi Infrastructure'da (EF Core, PBKDF2, JWT kütüphanesi).
// Application, JWT'nin nasıl üretildiğini veya şifrenin hangi algoritmayla hash'lendiğini BİLMEZ.

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct = default);
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
}

public interface IRefreshTokenRepository
{
    // Takip edilen (tracked) nesne döner: üzerinde yapılan değişiklik (RevokedAt) bir sonraki SaveChanges'te yazılır.
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);

    // Ekler ve kaydeder. Aynı DbContext'te takip edilen diğer değişiklikler (eski token'ın iptali)
    // de AYNI SaveChanges → AYNI transaction içinde yazılır. Rotation'ın atomik olması buna dayanır.
    Task AddAsync(RefreshToken token, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);

    Task RevokeAllForUserAsync(int userId, DateTime now, CancellationToken ct = default);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string passwordHash, string password);
}

public interface ITokenService
{
    AccessToken CreateAccessToken(User user);

    // Kriptografik olarak rastgele, tahmin edilemez değer (JWT DEĞİL; içinde bilgi yok).
    string GenerateRefreshToken();

    string HashRefreshToken(string refreshToken);

    TimeSpan RefreshTokenLifetime { get; }
}

public sealed record AccessToken(string Token, DateTime ExpiresAt);
