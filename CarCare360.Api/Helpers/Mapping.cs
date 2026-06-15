using CarCare360.Api.Models.Dtos;
using CarCare360.Api.Models.Entities;

namespace CarCare360.Api.Helpers;

/// <summary>
/// Методы-расширения для преобразования сущностей в DTO.
/// Предполагают, что нужные навигационные свойства уже загружены (Include).
/// </summary>
public static class Mapping
{
    /// <summary>Пользователь → UserDto.</summary>
    public static UserDto ToDto(this User u) => new()
    {
        UserID = u.UserID,
        Login = u.Login,
        FullName = u.FullName,
        Email = u.Email,
        Phone = u.Phone,
        Role = u.Role?.Name ?? string.Empty,
        IsActive = u.IsActive ?? false,
        CreatedAt = u.CreatedAt
    };

    /// <summary>Автомобиль → CarDto.</summary>
    public static CarDto ToDto(this Car c) => new()
    {
        CarID = c.CarID,
        ClientID = c.ClientID,
        ClientName = c.Client?.FullName,
        ModelID = c.ModelID,
        BrandName = c.Model?.Brand?.Name,
        ModelName = c.Model?.Name,
        Year = c.Year,
        VIN = c.VIN,
        LicensePlate = c.LicensePlate,
        Color = c.Color,
        Mileage = c.Mileage
    };

    /// <summary>Модель автомобиля → CarModelDto (требует загруженной навигации Brand).</summary>
    public static CarModelDto ToDto(this CarModel m) => new()
    {
        ModelID = m.ModelID,
        BrandID = m.BrandID,
        BrandName = m.Brand?.Name ?? string.Empty,
        Name = m.Name,
        YearFrom = m.YearFrom,
        YearTo = m.YearTo
    };

    /// <summary>Услуга → ServiceDto.</summary>
    public static ServiceDto ToDto(this Service s) => new()
    {
        ServiceID = s.ServiceID,
        Name = s.Name,
        Description = s.Description,
        NormHour = s.NormHour,
        BasePrice = s.BasePrice
    };

    /// <summary>Запчасть → PartDto.</summary>
    public static PartDto ToDto(this Part p) => new()
    {
        PartID = p.PartID,
        Name = p.Name,
        PartNumber = p.PartNumber,
        QuantityInStock = p.QuantityInStock,
        Price = p.Price
    };

    /// <summary>Запись аудита → AuditLogDto.</summary>
    public static AuditLogDto ToDto(this AuditLog a) => new()
    {
        LogID = a.LogID,
        TableName = a.TableName,
        Operation = a.Operation,
        PrimaryKeyValue = a.PrimaryKeyValue,
        ChangedBy = a.ChangedBy,
        ChangedAt = a.ChangedAt,
        OldValues = a.OldValues,
        NewValues = a.NewValues,
        IPAddress = a.IPAddress
    };

    /// <summary>Краткое описание автомобиля для вложенных ответов.</summary>
    public static string CarInfo(this Car c)
    {
        var brand = c.Model?.Brand?.Name;
        var model = c.Model?.Name;
        var name = string.Join(' ', new[] { brand, model }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return string.IsNullOrWhiteSpace(name) ? c.LicensePlate : $"{name} ({c.LicensePlate})";
    }
}
