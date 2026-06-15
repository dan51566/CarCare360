using System.ComponentModel.DataAnnotations;

namespace CarCare360.Api.Models.Dtos;

/// <summary>Услуга в составе заказа (для ответа).</summary>
public class OrderServiceDto
{
    public int OrderServiceID { get; set; }
    public int ServiceID { get; set; }
    public string? ServiceName { get; set; }
    public int? MechanicID { get; set; }
    public string? MechanicName { get; set; }
    public int? BayID { get; set; }
    public string? BayName { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Status { get; set; }
}

/// <summary>Запчасть в составе заказа (для ответа).</summary>
public class OrderPartDto
{
    public int OrderPartID { get; set; }
    public int PartID { get; set; }
    public string? PartName { get; set; }
    public int Quantity { get; set; }
    public decimal? PricePerUnit { get; set; }
}

/// <summary>Детальные данные заказа.</summary>
public class OrderDto
{
    public int OrderID { get; set; }
    public int CarID { get; set; }
    public string? CarInfo { get; set; }
    public int ClientID { get; set; }
    public string? ClientName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string? ScheduledTime { get; set; }
    public string? Status { get; set; }
    public int? Mileage { get; set; }
    public string? Notes { get; set; }

    /// <summary>Итоговая стоимость запчастей (Quantity * PricePerUnit).</summary>
    public decimal PartsTotal { get; set; }

    public List<OrderServiceDto> Services { get; set; } = new();
    public List<OrderPartDto> Parts { get; set; } = new();
}

/// <summary>Запрос на создание заказа (клиент).</summary>
public class OrderCreateRequest
{
    /// <summary>Автомобиль, по которому создаётся заказ.</summary>
    [Required]
    public int CarID { get; set; }

    /// <summary>Запланированная дата.</summary>
    public DateTime? ScheduledDate { get; set; }

    /// <summary>Запланированное время в формате "HH:mm".</summary>
    public string? ScheduledTime { get; set; }

    /// <summary>Комментарий к заказу.</summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>Список идентификаторов услуг для добавления в заказ.</summary>
    public List<int> ServiceIds { get; set; } = new();
}

/// <summary>Запрос на обновление заказа.</summary>
public class OrderUpdateRequest
{
    /// <summary>Комментарий (доступен клиенту и админу).</summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>Запланированная дата (только админ).</summary>
    public DateTime? ScheduledDate { get; set; }

    /// <summary>Запланированное время "HH:mm" (только админ).</summary>
    public string? ScheduledTime { get; set; }

    /// <summary>Пробег (только админ).</summary>
    public int? Mileage { get; set; }
}

/// <summary>Запрос на изменение статуса заказа.</summary>
public class OrderStatusUpdateRequest
{
    /// <summary>
    /// Новый статус. Допустимые значения:
    /// Новый, Назначен, В работе, Ожидает запчасти, Готов, Выдан, Отменён.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = string.Empty;
}
