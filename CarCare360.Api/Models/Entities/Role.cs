using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarCare360.Api.Models.Entities;

/// <summary>
/// Роль пользователя в системе: Администратор, Механик, Клиент.
/// </summary>
[Table("Roles")]
public class Role
{
    /// <summary>Первичный ключ роли.</summary>
    [Key]
    public int RoleID { get; set; }

    /// <summary>Название роли.</summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Описание роли (необязательно).</summary>
    [MaxLength(200)]
    public string? Description { get; set; }

    // Навигационное свойство: пользователи с данной ролью
    public ICollection<User> Users { get; set; } = new List<User>();
}
