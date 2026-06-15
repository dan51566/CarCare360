namespace CarCare360.Desktop.Helpers;

/// <summary>
/// Глобальная информация о вошедшем пользователе.
/// Заполняется при успешной авторизации (см. LoginViewModel)
/// и используется главным окном и разделами для RBAC.
/// </summary>
public static class CurrentUser
{
    public static int     UserID   { get; set; }
    public static string  FullName { get; set; } = string.Empty;
    public static string  RoleName { get; set; } = string.Empty;
    public static string  Login    { get; set; } = string.Empty;
    public static string? Phone    { get; set; }
    public static string? Email    { get; set; }

    public static void Logout()
    {
        UserID   = 0;
        FullName = string.Empty;
        RoleName = string.Empty;
        Login    = string.Empty;
        Phone    = null;
        Email    = null;
    }
}
