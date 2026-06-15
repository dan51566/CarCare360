using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarCare360.Api.Models.Entities;

/// <summary>
/// Марка автомобиля (Toyota, BMW, Lada и т.д.). Справочная таблица.
/// </summary>
[Table("CarBrands")]
public class CarBrand
{
    /// <summary>Первичный ключ марки.</summary>
    [Key]
    public int BrandID { get; set; }

    /// <summary>Название марки.</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Страна-производитель (необязательно).</summary>
    [MaxLength(100)]
    public string? Country { get; set; }

    // Навигационные свойства: модели данной марки
    public ICollection<CarModel> Models { get; set; } = new List<CarModel>();
}
