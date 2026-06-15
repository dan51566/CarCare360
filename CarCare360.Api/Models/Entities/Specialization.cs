using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarCare360.Api.Models.Entities;

/// <summary>
/// Специализация механика (Двигатель, Подвеска, Кузов и т.д.).
/// Справочная таблица.
/// </summary>
[Table("Specializations")]
public class Specialization
{
    /// <summary>Первичный ключ специализации.</summary>
    [Key]
    public int SpecID { get; set; }

    /// <summary>Название специализации.</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    // Навигационные свойства: механики с данной специализацией
    public ICollection<Mechanic> Mechanics { get; set; } = new List<Mechanic>();
}
