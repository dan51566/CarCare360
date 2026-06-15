using System.ComponentModel.DataAnnotations;

namespace CarCare360.Api.Models.Dtos;

/// <summary>Данные механика для ответов API.</summary>
public class MechanicDto
{
    public int MechanicID { get; set; }
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int? SpecializationID { get; set; }
    public string? SpecializationName { get; set; }
    public DateTime? HireDate { get; set; }
    public string? QualificationLevel { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Запрос на создание механика (создаёт пользователя + профиль).</summary>
public class MechanicCreateRequest
{
    [Required]
    [MaxLength(50)]
    public string Login { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(100)]
    public string? Email { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? Phone { get; set; }

    public int? SpecializationID { get; set; }

    public DateTime? HireDate { get; set; }

    [MaxLength(100)]
    public string? QualificationLevel { get; set; }
}

/// <summary>Элемент загрузки механика — назначенная услуга в составе заказа.</summary>
public class MechanicScheduleItemDto
{
    public int OrderServiceID { get; set; }
    public int OrderID { get; set; }
    public string? ServiceName { get; set; }
    public string? Status { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string? CarInfo { get; set; }
    public string? ClientName { get; set; }
}
