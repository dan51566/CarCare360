using System.ComponentModel.DataAnnotations;

namespace CarCare360.Api.Models.Dtos;

/// <summary>Данные автомобиля для ответов API.</summary>
public class CarDto
{
    public int CarID { get; set; }
    public int ClientID { get; set; }
    public string? ClientName { get; set; }
    public int ModelID { get; set; }
    public string? BrandName { get; set; }
    public string? ModelName { get; set; }
    public int? Year { get; set; }
    public string? VIN { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string? Color { get; set; }
    public int? Mileage { get; set; }
}

/// <summary>Запрос на добавление автомобиля.</summary>
public class CarCreateRequest
{
    /// <summary>Модель автомобиля (CarModels.ModelID).</summary>
    [Required]
    public int ModelID { get; set; }

    /// <summary>
    /// Клиент-владелец. Игнорируется для роли «Клиент» (привязка к себе),
    /// учитывается только для администратора.
    /// </summary>
    public int? ClientID { get; set; }

    public int? Year { get; set; }

    [MaxLength(50)]
    public string? VIN { get; set; }

    [Required]
    [MaxLength(20)]
    public string LicensePlate { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Color { get; set; }

    public int? Mileage { get; set; }
}

/// <summary>Запрос на обновление автомобиля.</summary>
public class CarUpdateRequest
{
    [Required]
    public int ModelID { get; set; }

    public int? Year { get; set; }

    [MaxLength(50)]
    public string? VIN { get; set; }

    [Required]
    [MaxLength(20)]
    public string LicensePlate { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Color { get; set; }

    public int? Mileage { get; set; }
}
