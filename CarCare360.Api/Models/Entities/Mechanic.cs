using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarCare360.Api.Models.Entities;

/// <summary>
/// Профиль механика — дополнение к учётной записи пользователя с ролью «Механик».
/// Хранит специализацию, дату найма и квалификацию.
/// </summary>
[Table("Mechanics")]
public class Mechanic
{
    /// <summary>Первичный ключ записи механика.</summary>
    [Key]
    public int MechanicID { get; set; }

    /// <summary>Внешний ключ пользователя-механика (Users.UserID).</summary>
    public int UserID { get; set; }

    /// <summary>Навигационное свойство: учётная запись механика.</summary>
    [ForeignKey(nameof(UserID))]
    public User? User { get; set; }

    /// <summary>Внешний ключ специализации. Может быть не задан.</summary>
    public int? SpecializationID { get; set; }

    /// <summary>Навигационное свойство: специализация механика.</summary>
    [ForeignKey(nameof(SpecializationID))]
    public Specialization? Specialization { get; set; }

    /// <summary>Дата принятия на работу.</summary>
    [Column(TypeName = "date")]
    public DateTime? HireDate { get; set; }

    /// <summary>Уровень квалификации (например: Junior, Senior, Master).</summary>
    [MaxLength(100)]
    public string? QualificationLevel { get; set; }

    /// <summary>Навигационное свойство: услуги, назначенные этому механику.</summary>
    public ICollection<OrderService> AssignedServices { get; set; } = new List<OrderService>();
}
