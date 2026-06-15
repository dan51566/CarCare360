using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarCare360.Desktop.Models;

/// <summary>
/// Бокс (пост) автосервиса — рабочее место для выполнения услуги.
/// В БД таблица называется ServiceBays, PK — BayID.
/// </summary>
[Table("ServiceBays")]
public class ServiceBay
{
    /// <summary>Первичный ключ бокса.</summary>
    [Key]
    public int BayID { get; set; }

    /// <summary>Название или номер бокса.</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Признак активности бокса (false — не используется).</summary>
    public bool? IsActive { get; set; } = true;

    // Навигационные свойства: услуги, выполняемые в данном боксе
    public ICollection<OrderService> OrderServices { get; set; } = new List<OrderService>();
}
