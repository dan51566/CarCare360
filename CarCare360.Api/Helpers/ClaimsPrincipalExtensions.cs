using System.Security.Claims;

namespace CarCare360.Api.Helpers;

/// <summary>
/// Методы-расширения для извлечения данных текущего пользователя из JWT-claims.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>Идентификатор пользователя (claim NameIdentifier). Бросает при отсутствии.</summary>
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(value, out var id))
            return id;
        throw ApiException.Forbidden("Не удалось определить пользователя из токена.");
    }

    /// <summary>Логин пользователя.</summary>
    public static string? GetLogin(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Name);

    /// <summary>Название роли пользователя.</summary>
    public static string? GetRole(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Role);

    /// <summary>Является ли пользователь администратором.</summary>
    public static bool IsAdmin(this ClaimsPrincipal user)
        => user.IsInRole(Roles.Admin);

    /// <summary>Является ли пользователь механиком.</summary>
    public static bool IsMechanic(this ClaimsPrincipal user)
        => user.IsInRole(Roles.Mechanic);
}
