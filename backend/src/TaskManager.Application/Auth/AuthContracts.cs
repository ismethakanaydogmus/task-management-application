namespace TaskManager.Application.Auth;

public record RegisterRequest(string Email, string Password);

public record LoginRequest(string Email, string Password);

public record UserResponse(int Id, string Email);

// İstemciye JSON gövdesinde dönen kısım. Refresh token BURADA YOK:
// o, controller tarafından httpOnly cookie olarak yazılır (JavaScript göremez).
public record AuthResponse(string AccessToken, DateTime AccessTokenExpiresAt, UserResponse User);

// Service → Controller arası iç sonuç. Controller, RefreshToken'ı cookie'ye, gerisini gövdeye koyar.
public sealed record AuthResult(
    AuthResponse Response,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
