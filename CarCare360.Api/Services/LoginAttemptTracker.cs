using System.Collections.Concurrent;

namespace CarCare360.Api.Services;

/// <summary>
/// Учёт неудачных попыток входа (в памяти процесса).
/// После достижения порога учётная запись блокируется (IsActive = 0) в AuthService.
/// Регистрируется как singleton.
/// </summary>
public class LoginAttemptTracker
{
    /// <summary>Порог неудачных попыток до блокировки.</summary>
    public const int MaxFailedAttempts = 3;

    private readonly ConcurrentDictionary<string, int> _failures = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Регистрирует неудачную попытку и возвращает текущее число неудач.</summary>
    public int RecordFailure(string login)
        => _failures.AddOrUpdate(login, 1, (_, current) => current + 1);

    /// <summary>Сбрасывает счётчик после успешного входа.</summary>
    public void Reset(string login) => _failures.TryRemove(login, out _);
}
