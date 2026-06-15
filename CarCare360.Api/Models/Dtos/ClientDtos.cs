using System.ComponentModel.DataAnnotations;

namespace CarCare360.Api.Models.Dtos;

/// <summary>Запрос на обновление профиля клиента.</summary>
public class ClientUpdateRequest
{
    /// <summary>ФИО.</summary>
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Email (необязательно).</summary>
    [EmailAddress]
    [MaxLength(100)]
    public string? Email { get; set; }

    /// <summary>Телефон (необязательно).</summary>
    [Phone]
    [MaxLength(20)]
    public string? Phone { get; set; }
}
