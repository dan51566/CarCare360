using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarCare360.Api.Models.Entities;

/// <summary>
/// Услуга в составе заказа.
/// Механик и бокс назначаются на уровне каждой услуги (не на заказ целиком).
/// Добавляется хранимой процедурой AddServiceToOrder.
/// Допустимые статусы (CHECK-ограничение в БД): Назначена, В работе, Выполнена, Отменена.
/// </summary>
[Table("OrderServices")]
public class OrderService
{
    /// <summary>Первичный ключ строки.</summary>
    [Key]
    public int OrderServiceID { get; set; }

    /// <summary>Внешний ключ заказа.</summary>
    public int OrderID { get; set; }

    /// <summary>Навигационное свойство: заказ.</summary>
    [ForeignKey(nameof(OrderID))]
    public Order? Order { get; set; }

    /// <summary>Внешний ключ услуги (Services.ServiceID).</summary>
    public int ServiceID { get; set; }

    /// <summary>Навигационное свойство: тип услуги.</summary>
    [ForeignKey(nameof(ServiceID))]
    public Service? Service { get; set; }

    /// <summary>Внешний ключ механика, назначенного на данную услугу (Mechanics.MechanicID).</summary>
    public int? MechanicID { get; set; }

    /// <summary>Навигационное свойство: механик.</summary>
    [ForeignKey(nameof(MechanicID))]
    public Mechanic? Mechanic { get; set; }

    /// <summary>Внешний ключ бокса, в котором выполняется услуга.</summary>
    public int? BayID { get; set; }

    /// <summary>Навигационное свойство: бокс.</summary>
    [ForeignKey(nameof(BayID))]
    public ServiceBay? Bay { get; set; }

    /// <summary>Фактическое время начала выполнения услуги.</summary>
    public DateTime? StartTime { get; set; }

    /// <summary>Фактическое время завершения услуги.</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>Статус выполнения услуги.</summary>
    [MaxLength(50)]
    public string? Status { get; set; }
}
