using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarCare360.Desktop.Models;

/// <summary>
/// Запись журнала аудита входов (Изменение №2, Доработка 4).
/// Фиксирует каждую попытку входа в десктопное приложение (успех/провал) и
/// время выхода. Пароль (введённый или хеш) НИКОГДА не сохраняется —
/// только логин, результат и время.
/// </summary>
[Table("LoginAuditLog")]
public class LoginAuditLog
{
    /// <summary>Первичный ключ (bigint — журнал растёт быстро).</summary>
    [Key]
    public long LogID { get; set; }

    /// <summary>Введённый логин (фиксируется даже если пользователь не существует).</summary>
    [Required]
    [MaxLength(100)]
    public string Login { get; set; } = string.Empty;

    /// <summary>Пользователь. NULL — если введённый логин не существует в системе.</summary>
    public int? UserID { get; set; }

    /// <summary>Результат попытки: 'S' — успех, 'F' — провал.</summary>
    [Required]
    [MaxLength(1)]
    public string Result { get; set; } = string.Empty;

    /// <summary>Дата и время попытки входа.</summary>
    public DateTime LoginAt { get; set; }

    /// <summary>
    /// Дата и время выхода. NULL — активная сессия либо аварийное завершение
    /// приложения (см. App.OnExit).
    /// </summary>
    public DateTime? LogoutAt { get; set; }
}
