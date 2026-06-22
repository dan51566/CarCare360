using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarCare360.Api.Models.Entities;

/// <summary>
/// Избранный механик клиента (Изменение №2, Доработка 3).
/// Связывает пользователя-клиента с механиком, которого он отметил как избранного.
/// Новая таблица FavoriteMechanics; существующие таблицы не изменяются.
/// </summary>
[Table("FavoriteMechanics")]
public class FavoriteMechanic
{
    /// <summary>Первичный ключ записи.</summary>
    [Key]
    public int FavoriteID { get; set; }

    /// <summary>Внешний ключ пользователя-клиента (Users.UserID).</summary>
    public int UserID { get; set; }

    /// <summary>Навигационное свойство: пользователь.</summary>
    [ForeignKey(nameof(UserID))]
    public User? User { get; set; }

    /// <summary>Внешний ключ механика (Mechanics.MechanicID).</summary>
    public int MechanicID { get; set; }

    /// <summary>Навигационное свойство: механик.</summary>
    [ForeignKey(nameof(MechanicID))]
    public Mechanic? Mechanic { get; set; }

    /// <summary>Дата и время добавления в избранное.</summary>
    public DateTime AddedAt { get; set; }
}
