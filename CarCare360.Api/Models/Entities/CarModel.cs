using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarCare360.Api.Models.Entities;

/// <summary>
/// Модель автомобиля (Camry, X5, Vesta и т.д.). Принадлежит марке CarBrand.
/// </summary>
[Table("CarModels")]
public class CarModel
{
    /// <summary>Первичный ключ модели.</summary>
    [Key]
    public int ModelID { get; set; }

    /// <summary>Внешний ключ марки.</summary>
    public int BrandID { get; set; }

    /// <summary>Навигационное свойство: марка автомобиля.</summary>
    [ForeignKey(nameof(BrandID))]
    public CarBrand? Brand { get; set; }

    /// <summary>Название модели.</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Год начала производства (необязательно).</summary>
    public int? YearFrom { get; set; }

    /// <summary>Год окончания производства (необязательно).</summary>
    public int? YearTo { get; set; }

    // Навигационные свойства: автомобили данной модели
    public ICollection<Car> Cars { get; set; } = new List<Car>();
}
