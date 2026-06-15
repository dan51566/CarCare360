using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarCare360.Desktop.Models;

/// <summary>
/// Запись журнала аудита.
/// Заполняется триггерами SQL Server — приложение только читает.
/// LogID — bigint (для больших объёмов).
/// </summary>
[Table("AuditLog")]
public class AuditLog
{
    /// <summary>Первичный ключ (bigint — большой объём записей).</summary>
    [Key]
    public long LogID { get; set; }

    /// <summary>Имя таблицы, в которой произошло изменение.</summary>
    [Required]
    [MaxLength(100)]
    public string TableName { get; set; } = string.Empty;

    /// <summary>Тип операции: I (INSERT), U (UPDATE), D (DELETE).</summary>
    [Required]
    [MaxLength(1)]
    public string Operation { get; set; } = string.Empty;

    /// <summary>Значение первичного ключа изменённой записи (в виде строки).</summary>
    [MaxLength(100)]
    public string? PrimaryKeyValue { get; set; }

    /// <summary>Пользователь, выполнивший изменение (логин или ФИО).</summary>
    [MaxLength(200)]
    public string? ChangedBy { get; set; }

    /// <summary>Дата и время изменения.</summary>
    public DateTime? ChangedAt { get; set; }

    /// <summary>Старые значения полей (формат JSON или текст).</summary>
    public string? OldValues { get; set; }

    /// <summary>Новые значения полей (формат JSON или текст).</summary>
    public string? NewValues { get; set; }

    /// <summary>IP-адрес клиента (если применимо).</summary>
    [MaxLength(50)]
    public string? IPAddress { get; set; }
}
