using System.Security.Claims;

namespace CarCare360.Api.Helpers;

/// <summary>
/// Сведения о текущем пользователе, передаваемые из контроллера в сервис.
/// Изолирует сервисный слой от типов ASP.NET (ClaimsPrincipal).
/// </summary>
public record CurrentUser(int UserId, string Role)
{
    public bool IsAdmin => Role == Roles.Admin;
    public bool IsMechanic => Role == Roles.Mechanic;
    public bool IsClient => Role == Roles.Client;

    /// <summary>Строит CurrentUser из ClaimsPrincipal.</summary>
    public static CurrentUser From(ClaimsPrincipal principal)
        => new(principal.GetUserId(), principal.GetRole() ?? string.Empty);
}
