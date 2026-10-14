namespace TaskManager.Application.Common.Exceptions;

// → 401 Unauthorized: "Kim olduğunu doğrulayamadım" (yanlış şifre, geçersiz/süresi dolmuş refresh token).
public sealed class UnauthorizedException(string message) : Exception(message);

// → 409 Conflict: İstek geçerli ama mevcut durumla çakışıyor (bu e-posta zaten kayıtlı).
public sealed class ConflictException(string message) : Exception(message);
