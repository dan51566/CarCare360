using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarCare360.Api.Models.Entities;

/// <summary>
/// Refresh-токен для обновления access-токена JWT.
/// Хранится в новой таблице RefreshTokens (не входит в исходные 14 таблиц БД).
/// Токен считается действительным, если он не отозван (RevokedAt IS NULL)
/// и не истёк (ExpiresAt > текущего момента).
/// </summary>
[Table("RefreshTokens")]
public class RefreshToken
{
    /// <summary>Первичный ключ (bigint).</summary>
    [Key]
    public long TokenID { get; set; }

    /// <summary>Внешний ключ пользователя-владельца токена.</summary>
    public int UserID { get; set; }

    /// <summary>Навигационное свойство: пользователь.</summary>
    [ForeignKey(nameof(UserID))]
    public User? User { get; set; }

    /// <summary>Значение токена (криптослучайная строка). Уникально и индексировано.</summary>
    [Required]
    [MaxLength(200)]
    public string Token { get; set; } = string.Empty;

    /// <summary>Дата и время истечения срока действия (7 дней с момента выдачи).</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Дата и время создания записи.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Дата и время отзыва токена (logout / ротация). NULL — токен активен.</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>Признак действительности токена: не отозван и не истёк.</summary>
    [NotMapped]
    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
}
