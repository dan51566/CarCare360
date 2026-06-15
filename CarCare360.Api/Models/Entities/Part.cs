using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarCare360.Api.Models.Entities;

/// <summary>
/// Запчасть на складе.
/// QuantityInStock уменьшается при добавлении в заказ (AddPartToOrder).
/// </summary>
[Table("Parts")]
public class Part
{
    /// <summary>Первичный ключ запчасти.</summary>
    [Key]
    public int PartID { get; set; }

    /// <summary>Название запчасти.</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Артикул / номер детали. Индексирован для поиска.</summary>
    [MaxLength(100)]
    public string? PartNumber { get; set; }

    /// <summary>Текущий остаток на складе (единиц).</summary>
    public int? QuantityInStock { get; set; }

    /// <summary>Цена за единицу (рублей).</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? Price { get; set; }

    // Навигационные свойства: позиции в заказах
    public ICollection<OrderPart> OrderParts { get; set; } = new List<OrderPart>();
}
