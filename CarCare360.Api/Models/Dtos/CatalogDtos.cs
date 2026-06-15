using System.ComponentModel.DataAnnotations;

namespace CarCare360.Api.Models.Dtos;

/// <summary>Данные услуги для ответов API.</summary>
public class ServiceDto
{
    public int ServiceID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal NormHour { get; set; }
    public decimal? BasePrice { get; set; }
}

/// <summary>Запрос на создание/обновление услуги.</summary>
public class ServiceUpsertRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Range(0, 1000)]
    public decimal NormHour { get; set; }

    [Range(0, 1_000_000)]
    public decimal? BasePrice { get; set; }
}

/// <summary>
/// Модель автомобиля для справочника (марка + модель + диапазон годов).
/// Используется мобильным приложением при добавлении автомобиля,
/// чтобы клиент выбирал готовый ModelID, а не вводил марку/модель вручную.
/// </summary>
public class CarModelDto
{
    public int ModelID { get; set; }
    public int BrandID { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? YearFrom { get; set; }
    public int? YearTo { get; set; }
}

/// <summary>Данные запчасти для ответов API.</summary>
public class PartDto
{
    public int PartID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PartNumber { get; set; }
    public int? QuantityInStock { get; set; }
    public decimal? Price { get; set; }
}

/// <summary>Запрос на создание/обновление запчасти.</summary>
public class PartUpsertRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? PartNumber { get; set; }

    [Range(0, int.MaxValue)]
    public int? QuantityInStock { get; set; }

    [Range(0, 1_000_000)]
    public decimal? Price { get; set; }
}
