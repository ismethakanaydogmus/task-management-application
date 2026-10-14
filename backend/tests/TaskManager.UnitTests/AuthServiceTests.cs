using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using TaskManager.Application.Auth;
using TaskManager.Application.Common.Exceptions;
using TaskManager.UnitTests.Fakes;

namespace TaskManager.UnitTests;

// AuthService'in güvenlik akışları: kayıt, giriş, refresh token ROTATION ve REUSE DETECTION.
public class AuthServiceTests
{
    private const string Password = "guclu-bir-sifre";

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 14, 11, 0, 0, TimeSpan.Zero));
    private readonly InMemoryUserRepository _users = new();
    private readonly InMemoryRefreshTokenRepository _tokens;
    private readonly FakePasswordHasher _hasher = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _tokens = new InMemoryRefreshTokenRepository(_users);
        _sut = new AuthService(
            _users,
            _tokens,
            _hasher,
            new FakeTokenService(),
            _clock,
            new RegisterRequestValidator(),
            new LoginRequestValidator(),
            NullLogger<AuthService>.Instance); // loglar testte bir yere yazılmasın
    }

    // ---------------------------------------------------------------- Kayıt

    [Fact]
    public async Task Register_NormalizesEmail_AndStoresHashNotPassword()
    {
        await _sut.RegisterAsync(new RegisterRequest("  Ali@Ornek.COM ", Password));

        var user = Assert.Single(_users.Users);
        Assert.Equal("ali@ornek.com", user.Email);
        Assert.NotEqual(Password, user.PasswordHash);
    }

    [Fact]
    public async Task Register_RejectsSameEmailWithDifferentCase()
    {
        await _sut.RegisterAsync(new RegisterRequest("ali@ornek.com", Password));

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.RegisterAsync(new RegisterRequest("ALI@ornek.com", Password)));
    }

    // ---------------------------------------------------------------- Giriş

    [Fact]
    public async Task Login_ReturnsSameError_ForUnknownEmailAndWrongPassword()
    {
        await _sut.RegisterAsync(new RegisterRequest("ali@ornek.com", Password));

        var unknownEmail = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _sut.LoginAsync(new LoginRequest("yok@ornek.com", Password)));
        var wrongPassword = await Assert.ThrowsAsync<UnauthorizedException>(
            () => _sut.LoginAsync(new LoginRequest("ali@ornek.com", "yanlis-sifre")));

        // User enumeration koruması: saldırgan mesajdan "bu e-posta kayıtlı mı" bilgisini çıkaramaz.
        Assert.Equal(unknownEmail.Message, wrongPassword.Message);
    }

    [Fact]
    public async Task Login_VerifiesAPasswordHash_EvenWhenUserDoesNotExist()
    {
        // Timing attack koruması: kullanıcı yokken de (sahte) hash doğrulaması YAPILMALI,
        // yoksa cevap süresi "kullanıcı yok" bilgisini sızdırır.
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _sut.LoginAsync(new LoginRequest("yok@ornek.com", Password)));

        Assert.Equal(1, _hasher.VerifyCallCount);
    }

    // ---------------------------------------------------------------- Refresh token rotation

    [Fact]
    public async Task Refresh_RotatesToken_AndRevokesTheOldOne()
    {
        var login = await _sut.RegisterAsync(new RegisterRequest("ali@ornek.com", Password));

        var refreshed = await _sut.RefreshAsync(login.RefreshToken);

        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
        var old = _tokens.Tokens.Single(t => t.TokenHash == $"h::{login.RefreshToken}");
        Assert.NotNull(old.RevokedAt);
        Assert.Equal($"h::{refreshed.RefreshToken}", old.ReplacedByTokenHash);
    }

    [Fact]
    public async Task Refresh_WithAlreadyRotatedToken_RevokesAllSessionsOfUser()
    {
        // Senaryo: Saldırgan Ali'nin refresh token'ını çaldı. Ali (veya saldırgan) onu bir kez kullandı → rotate edildi.
        var login = await _sut.RegisterAsync(new RegisterRequest("ali@ornek.com", Password));
        var legitimate = await _sut.RefreshAsync(login.RefreshToken);

        // Eski (çalınmış) token tekrar kullanılıyor.
        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.RefreshAsync(login.RefreshToken));

        // REUSE DETECTION: Meşru kullanıcının yeni token'ı da artık geçersiz (tüm oturumlar kapandı).
        Assert.All(_tokens.Tokens, t => Assert.NotNull(t.RevokedAt));
        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.RefreshAsync(legitimate.RefreshToken));
    }

    [Fact]
    public async Task Refresh_RejectsExpiredToken()
    {
        var login = await _sut.RegisterAsync(new RegisterRequest("ali@ornek.com", Password));

        _clock.Advance(TimeSpan.FromDays(8)); // ömrü 7 gün

        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.RefreshAsync(login.RefreshToken));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("hic-verilmemis-bir-token")]
    public async Task Refresh_RejectsMissingOrUnknownToken(string? token)
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.RefreshAsync(token));
    }

    [Fact]
    public async Task Logout_RevokesToken_SoItCannotBeRefreshed()
    {
        var login = await _sut.RegisterAsync(new RegisterRequest("ali@ornek.com", Password));

        await _sut.LogoutAsync(login.RefreshToken);

        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.RefreshAsync(login.RefreshToken));
    }
}
