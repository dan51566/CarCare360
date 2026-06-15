using System.IO;
using System.Text.Json;

namespace CarCare360.Desktop.Helpers;

/// <summary>
/// Сохраняет и восстанавливает последний использованный логин.
/// Файл хранится в %AppData%\CarCare360\saved_login.json.
/// Пароль никогда не сохраняется.
/// </summary>
public static class RememberLoginHelper
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CarCare360",
        "saved_login.json");

    public static string? Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            var json = File.ReadAllText(FilePath);
            var obj  = JsonSerializer.Deserialize<SavedLogin>(json);
            return obj?.Login;
        }
        catch { return null; }
    }

    public static void Save(string login)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var json = JsonSerializer.Serialize(new SavedLogin(login));
            File.WriteAllText(FilePath, json);
        }
        catch { /* не критично */ }
    }

    public static void Clear()
    {
        try { if (File.Exists(FilePath)) File.Delete(FilePath); }
        catch { /* не критично */ }
    }

    private sealed record SavedLogin(string Login);
}
