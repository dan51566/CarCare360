using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarCare360.Desktop.Models;

/// <summary>
/// Заказ-наряд. Механик и бокс назначаются НА КАЖДУЮ УСЛУГУ (OrderService),
/// а не на весь заказ — такова реальная схема БД.
/// Статусы: Принят, Диагностика, В работе, Ожидание запчастей, Готов, Выдан, Отменён.
/// </summary>
[Table("Orders")]
public class Order
{
    /// <summary>Первичный ключ заказа.</summary>
    [Key]
    public int OrderID { get; set; }

    /// <summary>Внешний ключ автомобиля.</summary>
    public int CarID { get; set; }

    /// <summary>Навигационное свойство: автомобиль.</summary>
    [ForeignKey(nameof(CarID))]
    public Car? Car { get; set; }

    /// <summary>Внешний ключ клиента (Users.UserID с ролью «Клиент»).</summary>
    public int ClientID { get; set; }

    /// <summary>Навигационное свойство: клиент-владелец заказа.</summary>
    [ForeignKey(nameof(ClientID))]
    public User? Client { get; set; }

    /// <summary>Дата и время создания записи (заполняется автоматически).</summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>Запланированная дата выполнения.</summary>
    [Column(TypeName = "date")]
    public DateTime? ScheduledDate { get; set; }

    /// <summary>Запланированное время выполнения.</summary>
    public TimeSpan? ScheduledTime { get; set; }

    /// <summary>
    /// Текущий статус заказа.
    /// Допустимые: Принят, Диагностика, В работе, Ожидание запчастей, Готов, Выдан, Отменён.
    /// </summary>
    [MaxLength(50)]
    public string? Status { get; set; } = "Принят";

    /// <summary>Пробег автомобиля на момент сдачи в сервис (км).</summary>
    public int? Mileage { get; set; }

    /// <summary>Примечания к заказу.</summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>Флаг мягкого удаления.</summary>
    public bool? IsDeleted { get; set; } = false;

    // Навигационные свойства
    public ICollection<OrderService> OrderServices { get; set; } = new List<OrderService>();
    public ICollection<OrderPart> OrderParts { get; set; } = new List<OrderPart>();
}
