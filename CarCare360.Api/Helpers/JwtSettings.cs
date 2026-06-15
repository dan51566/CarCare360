namespace CarCare360.Api.Helpers;

/// <summary>
/// Настройки JWT, считываемые из секции "Jwt" файла appsettings.json.
/// В продакшене Key следует хранить в User Secrets или переменных окружения.
/// </summary>
public class JwtSettings
{
    /// <summary>Имя секции конфигурации.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Секретный ключ для подписи токенов (минимум 32 байта).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Издатель токена.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Аудитория токена.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Время жизни access-токена в минутах.</summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>Время жизни refresh-токена в днях.</summary>
    public int RefreshTokenDays { get; set; } = 7;
}
