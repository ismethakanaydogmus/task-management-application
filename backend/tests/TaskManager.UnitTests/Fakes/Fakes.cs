using TaskManager.Application.Abstractions;
using TaskManager.Application.Tasks;
using TaskManager.Domain.Entities;

namespace TaskManager.UnitTests.Fakes;

// ELLE YAZILMIŞ TEST DOUBLE'LAR (FAKE).
//
// Fake = basit ama GERÇEKTEN ÇALIŞAN bir gerçekleştirim (burada: veriyi List'te tutan repository).
// Mock kütüphanesi (Moq, NSubstitute) yerine fake seçtik çünkü:
//  - Testler "repository.UpdateAsync 1 kez çağrıldı mı?" (NASIL) değil, "görev güncellendi mi?" (NE) diye sorar.
//    TaskService'in içini refactor edince (ör. iki çağrıyı birleştirince) testler kırılmaz.
//  - Okunması kolay; sihir yok.
// Mock ne zaman daha iyi? Bir yan etkinin GERÇEKLEŞTİĞİNİ doğrulamak gerektiğinde ("e-posta gönderildi mi?")
// veya fake yazmak çok zahmetliyse.
//
// ÖNEMLİ: Fake repository SQL'i test ETMEZ (ILIKE, CASE sıralama, gerçek sahiplik sorgusu...).
// Onlar integration testlerde, gerçek PostgreSQL ile test ediliyor.

public sealed class FakeCurrentUser(int userId) : ICurrentUser
{
    public int UserId { get; set; } = userId;
}

public sealed class InMemoryTaskRepository : ITaskRepository
{
    public List<TaskItem> Tasks { get; } = [];
    private int _nextId = 1;

    public Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> GetPagedAsync(int userId, TaskQuery query, CancellationToken ct = default)
    {
        var mine = Tasks.Where(t => t.UserId == userId).ToList();
        IReadOnlyList<TaskItem> page = mine.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList();
        return Task.FromResult((page, mine.Count));
    }

    public Task<TaskSummary> GetSummaryAsync(int userId, CancellationToken ct = default)
    {
        var mine = Tasks.Where(t => t.UserId == userId).ToList();
        var completed = mine.Count(t => t.IsCompleted);
        return Task.FromResult(new TaskSummary(mine.Count, mine.Count - completed, completed));
    }

    // Gerçek repository'deki sahiplik davranışını taklit eder: başkasının görevi → null.
    public Task<TaskItem?> GetByIdAsync(int id, int userId, CancellationToken ct = default)
        => Task.FromResult(Tasks.FirstOrDefault(t => t.Id == id && t.UserId == userId));

    public Task AddAsync(TaskItem task, CancellationToken ct = default)
    {
        task.Id = _nextId++;
        Tasks.Add(task);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TaskItem task, CancellationToken ct = default) => Task.CompletedTask; // nesne zaten listede

    public Task<bool> DeleteAsync(int id, int userId, CancellationToken ct = default)
        => Task.FromResult(Tasks.RemoveAll(t => t.Id == id && t.UserId == userId) > 0);
}

public sealed class InMemoryUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct = default)
        => Task.FromResult(Users.FirstOrDefault(u => u.Email == normalizedEmail));

    public Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
        => Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct = default)
        => Task.FromResult(Users.Any(u => u.Email == normalizedEmail));

    public Task AddAsync(User user, CancellationToken ct = default)
    {
        user.Id = Users.Count + 1;
        Users.Add(user);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryRefreshTokenRepository(InMemoryUserRepository users) : IRefreshTokenRepository
{
    public List<RefreshToken> Tokens { get; } = [];

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
    {
        var token = Tokens.FirstOrDefault(t => t.TokenHash == tokenHash);
        if (token is not null)
            token.User = users.Users.Single(u => u.Id == token.UserId); // EF'deki Include(t => t.User) taklidi
        return Task.FromResult(token);
    }

    public Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        token.Id = Tokens.Count + 1;
        Tokens.Add(token);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task RevokeAllForUserAsync(int userId, DateTime now, CancellationToken ct = default)
    {
        foreach (var t in Tokens.Where(t => t.UserId == userId && t.RevokedAt is null))
            t.RevokedAt = now;
        return Task.CompletedTask;
    }
}

// Gerçek PBKDF2 her çağrıda ~50-100 ms sürer; unit testte hash'in KENDİSİNİ değil AuthService'in
// akışını test ediyoruz. (Gerçek hasher integration testlerde zaten kullanılıyor.)
public sealed class FakePasswordHasher : IPasswordHasher
{
    public int VerifyCallCount { get; private set; }
    public string Hash(string password) => $"hashed::{password}";
    public bool Verify(string passwordHash, string password)
    {
        VerifyCallCount++;
        return passwordHash == Hash(password);
    }
}

public sealed class FakeTokenService : ITokenService
{
    private int _counter;
    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(7);
    public AccessToken CreateAccessToken(User user) => new($"access-{user.Id}-{++_counter}", DateTime.UtcNow.AddMinutes(15));
    public string GenerateRefreshToken() => $"refresh-{Guid.NewGuid():N}";
    public string HashRefreshToken(string refreshToken) => $"h::{refreshToken}";
}
