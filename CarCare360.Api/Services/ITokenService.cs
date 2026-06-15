using CarCare360.Api.Models.Entities;

namespace CarCare360.Api.Services;

/// <summary>
/// Генерация токенов JWT и refresh-токенов.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Создаёт подписанный access-токен JWT для пользователя.
    /// Требует, чтобы у пользователя была загружена навигация Role.
    /// </summary>
    /// <returns>Строка токена и момент истечения (UTC).</returns>
    (string token, DateTime expiresAt) CreateAccessToken(User user);

    /// <summary>Генерирует криптослучайный refresh-токен.</summary>
    string GenerateRefreshToken();
}
