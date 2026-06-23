using CarCare360.Desktop.Data;
using CarCare360.Desktop.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CarCare360.Desktop.ViewModels;

/// <summary>Строка журнала аудита входов (для DataGrid).</summary>
public sealed class LoginAuditRow
{
    public long      LogID        { get; init; }
    public string    Login        { get; init; } = string.Empty;
    public string    Result       { get; init; } = string.Empty;
    public DateTime  LoginAt      { get; init; }
    public DateTime? LogoutAt     { get; init; }

    /// <summary>true — логин помечен как подозрительный (5+ провалов подряд за 15 мин).</summary>
    public bool      IsSuspicious { get; init; }

    public bool   IsSuccess   => Result == "S";
    public string ResultLabel => Result == "S" ? "Успех" : "Провал";
}

/// <summary>
/// ViewModel раздела «Аудит входов» (Изменение №2, Доработка 4).
/// Доступен только администратору. Показывает попытки входа с фильтрами и
/// выделяет логины с признаком подбора пароля.
/// </summary>
public sealed class LoginAuditViewModel : BaseViewModel
{
    public static readonly string[] AllResults = ["Все", "Успех", "Провал"];

    /// <summary>Порог «подозрительной активности»: окно времени для серии провалов.</summary>
    private static readonly TimeSpan SuspiciousWindow = TimeSpan.FromMinutes(15);
    private const int SuspiciousFailures = 5;

    private string    _loginFilter    = string.Empty;
    private string    _selectedResult = "Все";
    private DateTime? _dateFrom;
    private DateTime? _dateTo;
    private bool      _isLoading;
    private string    _statusMessage  = string.Empty;
    private ObservableCollection<LoginAuditRow> _logs = [];

    public LoginAuditViewModel()
    {
        ApplyCommand = new RelayCommand(async () => await LoadAsync());
    }

    // ── Фильтры ───────────────────────────────────────────────────────────

    public string LoginFilter
    {
        get => _loginFilter;
        set => SetProperty(ref _loginFilter, value);
    }

    public string SelectedResult
    {
        get => _selectedResult;
        set => SetProperty(ref _selectedResult, value);
    }

    public DateTime? DateFrom
    {
        get => _dateFrom;
        set => SetProperty(ref _dateFrom, value);
    }

    public DateTime? DateTo
    {
        get => _dateTo;
        set => SetProperty(ref _dateTo, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set { if (SetProperty(ref _isLoading, value)) CommandManager.InvalidateRequerySuggested(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ObservableCollection<LoginAuditRow> Logs
    {
        get => _logs;
        private set => SetProperty(ref _logs, value);
    }

    public ICommand ApplyCommand { get; }

    // ── Загрузка ──────────────────────────────────────────────────────────

    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            await using var db = new CarCareDbContext();

            // ── Детект подозрительных логинов ──────────────────────────────
            // Считаем по СВЕЖИМ записям БЕЗ фильтра результата: иначе скрытые
            // успехи исказили бы детект. Строгий критерий по ТЗ: берём последние
            // 5 попыток конкретного логина; если их не меньше 5, все — провалы
            // (без единого 'S'), и самая старая из них уложилась в последние
            // 15 минут — логин подозрительный.
            var recent = await db.LoginAuditLogs
                .OrderByDescending(l => l.LoginAt)
                .Take(2000)
                .ToListAsync();

            var threshold = DateTime.Now - SuspiciousWindow;
            var suspicious = recent
                .GroupBy(l => l.Login)
                .Where(g =>
                {
                    var last5 = g.OrderByDescending(l => l.LoginAt).Take(SuspiciousFailures).ToList();
                    return last5.Count == SuspiciousFailures
                        && last5.All(l => l.Result == "F")
                        && last5.Min(l => l.LoginAt) >= threshold;
                })
                .Select(g => g.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // ── Отображаемый набор с фильтрами ─────────────────────────────
            var q = db.LoginAuditLogs.AsQueryable();

            var search = LoginFilter.Trim();
            if (!string.IsNullOrEmpty(search))
                q = q.Where(l => l.Login.Contains(search));

            if (SelectedResult == "Успех")
                q = q.Where(l => l.Result == "S");
            else if (SelectedResult == "Провал")
                q = q.Where(l => l.Result == "F");

            if (DateFrom.HasValue)
                q = q.Where(l => l.LoginAt >= DateFrom.Value);

            if (DateTo.HasValue)
                q = q.Where(l => l.LoginAt < DateTo.Value.AddDays(1));

            var raw = await q
                .OrderByDescending(l => l.LoginAt)
                .Take(500)
                .ToListAsync();

            Logs = new ObservableCollection<LoginAuditRow>(raw.Select(l => new LoginAuditRow
            {
                LogID        = l.LogID,
                Login        = l.Login,
                Result       = l.Result,
                LoginAt      = l.LoginAt,
                LogoutAt     = l.LogoutAt,
                IsSuspicious = suspicious.Contains(l.Login)
            }));

            var suspCount = suspicious.Count;
            StatusMessage = $"Записей: {Logs.Count}"
                + (suspCount > 0 ? $"  •  подозрительных логинов: {suspCount}" : string.Empty);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
