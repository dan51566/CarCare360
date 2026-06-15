using System.IO;
using System.Text.Json;

namespace CarCare360.Desktop.Helpers;

/// <summary>
/// Stores per-user avatar image paths in %AppData%\CarCare360\avatars.json.
/// No DB migration required — purely local file-based storage.
/// </summary>
public static class UserAvatarStorage
{
    private static readonly string SettingsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "CarCare360", "avatars.json");

    public static string? GetAvatarPath(int userId)
    {
        try
        {
            if (!File.Exists(SettingsPath)) return null;
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(
                           File.ReadAllText(SettingsPath));
            return dict?.TryGetValue(userId.ToString(), out var p) == true ? p : null;
        }
        catch { return null; }
    }

    /// <summary>Fired after a user's avatar path is saved or cleared. Arg = UserID.</summary>
    public static event EventHandler<int>? AvatarChanged;

    public static void SaveAvatarPath(int userId, string? path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            Dictionary<string, string> dict = new();
            if (File.Exists(SettingsPath))
                dict = JsonSerializer.Deserialize<Dictionary<string, string>>(
                           File.ReadAllText(SettingsPath)) ?? new();

            if (path is null) dict.Remove(userId.ToString());
            else              dict[userId.ToString()] = path;

            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(dict));
        }
        catch { }

        AvatarChanged?.Invoke(null, userId);
    }
}
