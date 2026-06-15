using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarCare360.Api.Models.Entities;

/// <summary>
/// Пользователь системы.
/// Хранит и сотрудников (Администратор, Механик), и клиентов (роль «Клиент»).
/// PasswordHash хранится как BINARY(64) — BCrypt-хеш в виде ASCII-байт.
/// Пароль в открытом виде НИКОГДА не сохраняется.
/// </summary>
[Table("Users")]
public class User
{
    /// <summary>Первичный ключ пользователя.</summary>
    [Key]
    public int UserID { get; set; }

    /// <summary>Уникальный логин для входа в систему.</summary>
    [Required]
    [MaxLength(50)]
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Хеш пароля — BCrypt-строка, сохранённая как ASCII-байты в BINARY(64).
    /// Для проверки: BCrypt.Verify(password, Encoding.ASCII.GetString(hash).TrimEnd('\0')).
    /// </summary>
    [Required]
    [MaxLength(64)]
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

    /// <summary>Полное имя (ФИО).</summary>
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Email адрес (необязательно).</summary>
    [MaxLength(100)]
    public string? Email { get; set; }

    /// <summary>Номер телефона (необязательно).</summary>
    [MaxLength(20)]
    public string? Phone { get; set; }

    /// <summary>Внешний ключ роли.</summary>
    public int RoleID { get; set; }

    /// <summary>Навигационное свойство: роль пользователя.</summary>
    [ForeignKey(nameof(RoleID))]
    public Role? Role { get; set; }

    /// <summary>
    /// Признак активности учётной записи.
    /// false — заблокирована (например, после 3 неудачных попыток входа).
    /// </summary>
    public bool? IsActive { get; set; } = true;

    /// <summary>Дата и время создания записи.</summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>Флаг мягкого удаления (для клиентов).</summary>
    public bool? IsDeleted { get; set; } = false;

    // ===== Навигационные свойства =====

    /// <summary>Автомобили, принадлежащие этому пользователю (клиенту).</summary>
    public ICollection<Car> Cars { get; set; } = new List<Car>();

    /// <summary>Заказы, оформленные на этого клиента.</summary>
    public ICollection<Order> ClientOrders { get; set; } = new List<Order>();

    /// <summary>Профиль механика (если пользователь — механик).</summary>
    public Mechanic? MechanicProfile { get; set; }

    /// <summary>Refresh-токены, выданные пользователю.</summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
