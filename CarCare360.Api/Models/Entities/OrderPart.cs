using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarCare360.Api.Models.Entities;

/// <summary>
/// Запчасть в составе заказа.
/// Добавляется хранимой процедурой AddPartToOrder (одновременно списывается со склада).
/// </summary>
[Table("OrderParts")]
public class OrderPart
{
    /// <summary>Первичный ключ строки.</summary>
    [Key]
    public int OrderPartID { get; set; }

    /// <summary>Внешний ключ заказа.</summary>
    public int OrderID { get; set; }

    /// <summary>Навигационное свойство: заказ.</summary>
    [ForeignKey(nameof(OrderID))]
    public Order? Order { get; set; }

    /// <summary>Внешний ключ запчасти.</summary>
    public int PartID { get; set; }

    /// <summary>Навигационное свойство: запчасть.</summary>
    [ForeignKey(nameof(PartID))]
    public Part? Part { get; set; }

    /// <summary>Количество использованных единиц.</summary>
    public int Quantity { get; set; }

    /// <summary>Зафиксированная цена за единицу на момент добавления (рублей).</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? PricePerUnit { get; set; }
}
