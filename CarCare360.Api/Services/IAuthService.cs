using CarCare360.Api.Models.Dtos;

namespace CarCare360.Api.Services;

/// <summary>
/// Сервис аутентификации: вход, регистрация клиента, обновление и отзыв токенов.
/// </summary>
public interface IAuthService
{
    /// <summary>Авторизация по логину и паролю. Возвращает токены и данные пользователя.</summary>
    Task<AuthResponse> LoginAsync(LoginRequest request);

    /// <summary>Регистрация нового клиента (через хранимую процедуру RegisterClient).</summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    /// <summary>Обновление access-токена по действующему refresh-токену (с ротацией).</summary>
    Task<AuthResponse> RefreshAsync(string refreshToken);

    /// <summary>Выход — отзыв refresh-токена.</summary>
    Task LogoutAsync(string refreshToken);
}
