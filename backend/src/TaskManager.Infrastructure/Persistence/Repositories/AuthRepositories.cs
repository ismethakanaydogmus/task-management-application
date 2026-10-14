using Microsoft.EntityFrameworkCore;
using Npgsql;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Repositories;

internal sealed class EfUserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct = default)
        => db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

    public Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
        => db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct = default)
        => db.Users.AnyAsync(u => u.Email == normalizedEmail, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        // 23505 = PostgreSQL "unique_violation". İki kayıt isteği aynı anda EmailExists kontrolünü geçtiyse
        // ikincisi burada patlar. Bunu 500 değil, anlamlı bir 409'a çeviriyoruz.
        // (Npgsql'e özgü bu bilgi Infrastructure'da kalıyor; Application sadece ConflictException'ı bilir.)
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("Bu e-posta adresi zaten kayıtlı.");
        }
    }
}

internal sealed class EfRefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
        => db.RefreshTokens
            .Include(t => t.User) // yeni access token üretmek için kullanıcı bilgisi lazım (tek sorguda JOIN)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public Task RevokeAllForUserAsync(int userId, DateTime now, CancellationToken ct = default)
        // ExecuteUpdateAsync: nesneleri belleğe çekmeden tek bir "UPDATE ... WHERE" sorgusu.
        => db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
}
