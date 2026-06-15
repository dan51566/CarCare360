namespace CarCare360.Api.Helpers;

/// <summary>
/// Константы названий ролей. Совпадают со значениями столбца Roles.Name в БД.
/// Используются в атрибутах [Authorize(Roles = ...)] и при выдаче JWT.
/// </summary>
public static class Roles
{
    /// <summary>Администратор — полный доступ к системе.</summary>
    public const string Admin = "Администратор";

    /// <summary>Механик — выполнение и просмотр заказов.</summary>
    public const string Mechanic = "Механик";

    /// <summary>Клиент — основная роль для мобильного приложения.</summary>
    public const string Client = "Клиент";

    /// <summary>Сотрудники (админ и механик) — для эндпоинтов, недоступных клиенту.</summary>
    public const string Staff = Admin + "," + Mechanic;
}
