using System.Text;

namespace CarCare360.Api.Helpers;

/// <summary>
/// Хеширование и проверка паролей через BCrypt.
///
/// ВАЖНО: схема БД хранит PasswordHash как BINARY(64) — BCrypt-строка (60 ASCII-символов),
/// дополненная нулями до 64 байт. Логика полностью совпадает с десктопным приложением
/// (DatabaseSeeder.HashToBytes/BytesToHash), поэтому учётные записи совместимы между
/// десктопом и API: пользователь, созданный в одном, входит в другом.
/// </summary>
public static class PasswordHelper
{
    /// <summary>Фактор сложности BCrypt (как в десктопном приложении).</summary>
    private const int WorkFactor = 12;

    /// <summary>
    /// Конвертирует BCrypt-строку (60 символов ASCII) в byte[64] для хранения в BINARY(64).
    /// </summary>
    public static byte[] HashToBytes(string bcryptHash)
    {
        var bytes = new byte[64];
        var ascii = Encoding.ASCII.GetBytes(bcryptHash);
        // Копируем хеш (60 байт) в начало буфера; остаток заполняется нулями
        Array.Copy(ascii, bytes, Math.Min(ascii.Length, 64));
        return bytes;
    }

    /// <summary>
    /// Конвертирует byte[64] обратно в BCrypt-строку для вызова BCrypt.Verify().
    /// </summary>
    public static string BytesToHash(byte[] bytes)
        => Encoding.ASCII.GetString(bytes).TrimEnd('\0');

    /// <summary>
    /// Хеширует пароль и возвращает результат в формате BINARY(64) для сохранения в БД.
    /// </summary>
    public static byte[] Hash(string password)
        => HashToBytes(BCrypt.Net.BCrypt.HashPassword(password, WorkFactor));

    /// <summary>
    /// Проверяет пароль против хеша, прочитанного из столбца BINARY(64).
    /// </summary>
    public static bool Verify(string password, byte[] storedHash)
    {
        if (storedHash is null || storedHash.Length == 0)
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, BytesToHash(storedHash));
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Повреждённый или несовместимый хеш — считаем пароль неверным
            return false;
        }
    }
}
