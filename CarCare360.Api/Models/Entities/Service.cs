using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarCare360.Api.Models.Entities;

/// <summary>
/// Тип услуги автосервиса (Замена масла, Диагностика, Шиномонтаж и т.д.).
/// Стоимость = BasePrice * NormHour. В БД таблица называется Services, PK — ServiceID.
/// </summary>
[Table("Services")]
public class Service
{
    /// <summary>Первичный ключ услуги.</summary>
    [Key]
    public int ServiceID { get; set; }

    /// <summary>Название услуги.</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Описание услуги (необязательно).</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Нормативное количество часов на выполнение.</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal NormHour { get; set; }

    /// <summary>Базовая цена за нормо-час (рублей). Может быть не задана.</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? BasePrice { get; set; }

    // Навигационные свойства: позиции в заказах с данной услугой
    public ICollection<OrderService> OrderServices { get; set; } = new List<OrderService>();
}
