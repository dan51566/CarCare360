using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarCare360.Desktop.Models;

/// <summary>
/// Автомобиль клиента.
/// Марка автомобиля определяется через CarModel.Brand (не прямым FK).
/// LicensePlate и VIN индексированы для быстрого поиска.
/// </summary>
[Table("Cars")]
public class Car
{
    /// <summary>Первичный ключ автомобиля.</summary>
    [Key]
    public int CarID { get; set; }

    /// <summary>Внешний ключ клиента-владельца (Users.UserID с ролью «Клиент»).</summary>
    public int ClientID { get; set; }

    /// <summary>Навигационное свойство: клиент-владелец.</summary>
    [ForeignKey(nameof(ClientID))]
    public User? Client { get; set; }

    /// <summary>Внешний ключ модели автомобиля. Марка определяется через CarModel.BrandID.</summary>
    public int ModelID { get; set; }

    /// <summary>Навигационное свойство: модель (и через неё — марка).</summary>
    [ForeignKey(nameof(ModelID))]
    public CarModel? Model { get; set; }

    /// <summary>Год выпуска.</summary>
    public int? Year { get; set; }

    /// <summary>VIN-номер (17 символов). Индексирован для поиска.</summary>
    [MaxLength(50)]
    public string? VIN { get; set; }

    /// <summary>Государственный регистрационный номер. Индексирован для поиска.</summary>
    [Required]
    [MaxLength(20)]
    public string LicensePlate { get; set; } = string.Empty;

    /// <summary>Цвет автомобиля.</summary>
    [MaxLength(50)]
    public string? Color { get; set; }

    /// <summary>Пробег при последнем обращении (км).</summary>
    public int? Mileage { get; set; }

    // Навигационные свойства: заказы на этот автомобиль
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
