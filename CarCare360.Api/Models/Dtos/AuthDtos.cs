using System.ComponentModel.DataAnnotations;

namespace CarCare360.Api.Models.Dtos;

/// <summary>Запрос на авторизацию.</summary>
public class LoginRequest
{
    /// <summary>Логин пользователя.</summary>
    [Required]
    [MaxLength(50)]
    public string Login { get; set; } = string.Empty;

    /// <summary>Пароль в открытом виде (передаётся только по HTTPS).</summary>
    [Required]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Запрос на регистрацию нового клиента.</summary>
public class RegisterRequest
{
    /// <summary>Желаемый логин (должен быть уникален).</summary>
    [Required]
    [MaxLength(50)]
    public string Login { get; set; } = string.Empty;

    /// <summary>Пароль (минимум 6 символов).</summary>
    [Required]
    [MinLength(6)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    /// <summary>ФИО клиента.</summary>
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Email (необязательно).</summary>
    [EmailAddress]
    [MaxLength(100)]
    public string? Email { get; set; }

    /// <summary>Телефон (необязательно).</summary>
    [Phone]
    [MaxLength(20)]
    public string? Phone { get; set; }
}

/// <summary>Запрос на обновление access-токена.</summary>
public class RefreshRequest
{
    /// <summary>Действующий refresh-токен.</summary>
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>Запрос на выход (инвалидацию refresh-токена).</summary>
public class LogoutRequest
{
    /// <summary>Refresh-токен, который нужно отозвать.</summary>
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>Ответ на успешную авторизацию/обновление токена.</summary>
public class AuthResponse
{
    /// <summary>JWT access-токен (срок жизни ~15 минут).</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Refresh-токен (срок жизни ~7 дней).</summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>Момент истечения access-токена (UTC).</summary>
    public DateTime AccessTokenExpiresAt { get; set; }

    /// <summary>Данные авторизованного пользователя.</summary>
    public UserDto User { get; set; } = new();
}

/// <summary>Данные пользователя для ответов API (без пароля).</summary>
public class UserDto
{
    public int UserID { get; set; }
    public string Login { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
}
