using CarCare360.Desktop.Data;
using CarCare360.Desktop.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace CarCare360.Desktop.Helpers;

/// <summary>
/// Журналирование попыток входа (Изменение №2, Доработка 4).
///
/// ВАЖНО: пароль (введённый или хеш) НИКОГДА не передаётся в этот сервис и не
/// сохраняется — только логин, результат ('S'/'F') и время.
///
/// Все операции обёрнуты в изолированный try/catch: сбой журналирования НЕ
/// должен мешать входу пользователя при корректном пароле — методы никогда не
/// бросают исключение, а лишь логируют его в файл.
/// </summary>
public static class LoginAuditService
{
    /// <summary>Результат «успешный вход».</summary>
    public const string Success = "S";

    /// <summary>Результат «неудачный вход».</summary>
    public const string Failed = "F";

    /// <summary>
    /// LogID записи об успешном входе текущей сессии приложения — для проставления
    /// LogoutAt при выходе. 0 — нет активной залогированной сессии.
    /// Статик корректен: десктопный WPF — один экземпляр приложения.
    /// </summary>
    public static long CurrentSessionLogId { get; private set; }

    /// <summary>
    /// Записывает попытку входа. Возвращает LogID созданной записи (0 — при сбое).
    /// Вызывается синхронно с проверкой пароля, до открытия главного окна.
    /// </summary>
    /// <param name="login">Введённый логин (пароль сюда НЕ передаётся).</param>
    /// <param name="result"><see cref="Success"/> или <see cref="Failed"/>.</param>
    /// <param name="userId">Идентификатор пользователя или null, если логин не существует.</param>
    public static async Task<long> RecordAttemptAsync(string login, string result, int? userId)
    {
        try
        {
            await using var db = new CarCareDbContext();
            var entry = new LoginAuditLog
            {
                Login   = (login ?? string.Empty).Trim(),
                UserID  = userId,
                Result  = result,
                LoginAt = DateTime.Now
            };
            db.LoginAuditLogs.Add(entry);
            await db.SaveChangesAsync();

            if (result == Success)
                CurrentSessionLogId = entry.LogID;

            return entry.LogID;
        }
        catch (Exception ex)
        {
            WriteError(ex);
            return 0;
        }
    }

    /// <summary>
    /// Проставляет LogoutAt для записи текущей сессии (если она есть и ещё не закрыта).
    /// Синхронный — вызывается при выходе/закрытии приложения (быстрое обновление одной
    /// строки, без блокировки async на UI-потоке).
    ///
    /// После записи сбрасывает <see cref="CurrentSessionLogId"/> в 0, чтобы повторный
    /// вход в той же сессии Windows не унаследовал старый LogID.
    /// </summary>
    public static void RecordLogout()
    {
        var logId = CurrentSessionLogId;
        if (logId == 0)
            return;

        try
        {
            using var db = new CarCareDbContext();
            var entry = db.LoginAuditLogs.Find(logId);
            if (entry is not null && entry.LogoutAt is null)
            {
                entry.LogoutAt = DateTime.Now;
                db.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            WriteError(ex);
        }
        finally
        {
            CurrentSessionLogId = 0;
        }
    }

    /// <summary>Пишет ошибку журналирования в файл рядом с exe (не бросает исключений).</summary>
    private static void WriteError(Exception ex)
    {
        try
        {
            File.AppendAllText(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "login_audit_error.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}: {ex.Message}\n");
        }
        catch { /* журналирование аудита не должно ломать вход */ }
    }
}
