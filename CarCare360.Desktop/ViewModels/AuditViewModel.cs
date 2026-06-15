using CarCare360.Desktop.Data;
using CarCare360.Desktop.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;

namespace CarCare360.Desktop.ViewModels;

/// <summary>One field-level change shown in the expanded diff row.</summary>
public sealed record AuditDiffLine(string Field, string OldValue, string NewValue);

/// <summary>
/// DTO строки журнала аудита.
/// Mutable (extends BaseViewModel) для поддержки inline-раскрытия деталей.
/// </summary>
public sealed class AuditRowDto : BaseViewModel
{
    private bool _isExpanded;

    public long      LogID           { get; init; }
    public string    TableName       { get; init; } = string.Empty;
    public string    Operation       { get; init; } = string.Empty;
    public string?   PrimaryKeyValue { get; init; }
    public string?   ChangedBy       { get; init; }
    public DateTime? ChangedAt       { get; init; }
    public string?   OldValues       { get; init; }
    public string?   NewValues       { get; init; }

    public bool HasDetails => OldValues is not null || NewValues is not null;

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    // ── Human-readable labels ─────────────────────────────────────────────

    public string OperationLabel => Operation switch
    {
        "I" => "Добавление",
        "U" => "Изменение",
        "D" => "Удаление",
        _   => Operation,
    };

    public string TableDisplayName => TableName switch
    {
        "Users"          => "Пользователи",
        "Cars"           => "Автомобили",
        "Orders"         => "Заказы",
        "Parts"          => "Запчасти",
        "Services"       => "Услуги",
        "ServiceBays"    => "Боксы",
        "Specializations"=> "Специализации",
        "CarBrands"      => "Марки",
        "CarModels"      => "Модели авто",
        "Mechanics"      => "Механики",
        "OrderServices"  => "Услуги заказа",
        "OrderParts"     => "Запчасти заказа",
        _                => TableName,
    };

    /// <summary>One-line summary shown inline in the DataGrid row.</summary>
    public string ChangeSummary
    {
        get
        {
            if (!HasDetails) return string.Empty;

            var oldMap = TryParseJson(OldValues);
            var newMap = TryParseJson(NewValues);

            if (Operation == "U" && oldMap is not null && newMap is not null)
            {
                var changed = newMap.Keys
                    .Where(k => k != "PasswordHash")
                    .Where(k =>
                    {
                        var ov = oldMap.TryGetValue(k, out var oe) ? FormatVal(oe) : "—";
                        return ov != FormatVal(newMap[k]);
                    })
                    .Select(FriendlyName)
                    .ToList();
                return changed.Count > 0
                    ? "Изменено: " + string.Join(", ", changed)
                    : string.Empty;
            }
            if (Operation == "I" && newMap is not null)
            {
                var label = newMap.TryGetValue("FullName", out var fn) ? $"«{FormatVal(fn)}»"
                          : newMap.TryGetValue("Login",    out var lg) ? $"логин «{FormatVal(lg)}»"
                          : newMap.TryGetValue("Name",     out var nm) ? $"«{FormatVal(nm)}»"
                          : $"ID {PrimaryKeyValue}";
                return $"Добавлено: {label}";
            }
            if (Operation == "D" && oldMap is not null)
            {
                var label = oldMap.TryGetValue("FullName", out var fn) ? $"«{FormatVal(fn)}»"
                          : oldMap.TryGetValue("Login",    out var lg) ? $"логин «{FormatVal(lg)}»"
                          : oldMap.TryGetValue("Name",     out var nm) ? $"«{FormatVal(nm)}»"
                          : $"ID {PrimaryKeyValue}";
                return $"Удалено: {label}";
            }
            return string.Empty;
        }
    }

    /// <summary>Field-level diff rows for the expanded RowDetailsTemplate.</summary>
    public List<AuditDiffLine> DiffLines
    {
        get
        {
            var oldMap = TryParseJson(OldValues);
            var newMap = TryParseJson(NewValues);
            if (oldMap is null && newMap is null) return [];

            var lines = new List<AuditDiffLine>();

            if (Operation == "U" && oldMap is not null && newMap is not null)
            {
                foreach (var kv in newMap)
                {
                    if (kv.Key == "PasswordHash") { lines.Add(new("Пароль", "●●●●●●", "●●●●●●")); continue; }
                    var ov = oldMap.TryGetValue(kv.Key, out var oe) ? FormatVal(oe) : "—";
                    var nv = FormatVal(kv.Value);
                    if (ov != nv) lines.Add(new(FriendlyName(kv.Key), ov, nv));
                }
            }
            else if (Operation == "I" && newMap is not null)
            {
                foreach (var kv in newMap)
                {
                    if (kv.Key == "PasswordHash") continue;
                    lines.Add(new(FriendlyName(kv.Key), string.Empty, FormatVal(kv.Value)));
                }
            }
            else if (Operation == "D" && oldMap is not null)
            {
                foreach (var kv in oldMap)
                {
                    if (kv.Key == "PasswordHash") continue;
                    lines.Add(new(FriendlyName(kv.Key), FormatVal(kv.Value), string.Empty));
                }
            }

            return lines;
        }
    }

    public bool HasDiffLines => DiffLines.Count > 0;

    // ── Helpers ───────────────────────────────────────────────────────────

    private static readonly Dictionary<string, string> _fieldNames =
        new(StringComparer.OrdinalIgnoreCase)
    {
        // Users
        ["FullName"]          = "ФИО",
        ["Login"]             = "Логин",
        ["Email"]             = "Email",
        ["Phone"]             = "Телефон",
        ["IsActive"]          = "Активен",
        ["IsDeleted"]         = "Удалён",
        ["RoleID"]            = "Роль",
        ["CreatedAt"]         = "Дата создания",
        // Cars
        ["LicensePlate"]      = "Гос. номер",
        ["VIN"]               = "VIN",
        ["Year"]              = "Год выпуска",
        ["Mileage"]           = "Пробег (км)",
        ["BrandID"]           = "Марка",
        ["ModelID"]           = "Модель",
        ["UserID"]            = "Владелец",
        ["Color"]             = "Цвет",
        // Orders
        ["Status"]            = "Статус",
        ["TotalCost"]         = "Сумма (₽)",
        ["PlannedDate"]       = "Запланировано",
        ["CompletedDate"]     = "Завершено",
        ["Notes"]             = "Примечания",
        ["ClientID"]          = "Клиент",
        ["CarID"]             = "Автомобиль",
        ["BayID"]             = "Бокс",
        // Parts / Warehouse
        ["Name"]              = "Название",
        ["Price"]             = "Цена (₽)",
        ["Stock"]             = "Остаток",
        ["Unit"]              = "Единица",
        ["Description"]       = "Описание",
        // Mechanics
        ["HireDate"]          = "Дата найма",
        ["Salary"]            = "Зарплата",
        ["SpecializationID"]  = "Специализация",
        ["MechanicID"]        = "Механик",
        // ServiceBays
        ["BayNumber"]         = "Номер бокса",
        // Services
        ["Duration"]          = "Длительность",
        ["ServiceID"]         = "Услуга",
        // Order rows
        ["Quantity"]          = "Количество",
        ["PartID"]            = "Запчасть",
        ["OrderID"]           = "Заказ",
    };

    private static string FriendlyName(string key) =>
        _fieldNames.TryGetValue(key, out var n) ? n : key;

    private static string FormatVal(JsonElement el)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Null:   return "—";
            case JsonValueKind.True:   return "Да";
            case JsonValueKind.False:  return "Нет";
            case JsonValueKind.String:
                var s = el.GetString() ?? string.Empty;
                if (string.IsNullOrEmpty(s)) return "—";
                if (DateTime.TryParse(s, out var dt)) return dt.ToString("dd.MM.yyyy HH:mm");
                return s;
            default:
                return el.GetRawText();
        }
    }

    private static Dictionary<string, JsonElement>? TryParseJson(string? json)
    {
        if (json is null) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
            return doc.RootElement.EnumerateObject()
                      .ToDictionary(p => p.Name, p => p.Value.Clone());
        }
        catch { return null; }
    }
}

/// <summary>
/// ViewModel раздела «Аудит».
/// DatePicker-фильтры, постраничная загрузка, inline-раскрытие diff.
/// </summary>
public sealed class AuditViewModel : BaseViewModel
{
    public static readonly string[] AllTables = [
        "Все", "Users", "Cars", "Orders", "Parts",
        "Services", "ServiceBays", "Specializations",
        "CarBrands", "CarModels", "Mechanics", "OrderServices", "OrderParts"
    ];
    public static readonly string[] AllOperations = ["Все", "I", "U", "D"];

    private const int PageSize = 100;

    private string     _searchText        = string.Empty;
    private string     _selectedTable     = "Все";
    private string     _selectedOperation = "Все";
    private DateTime?  _dateFrom;
    private DateTime?  _dateTo;
    private bool       _isLoading;
    private bool       _hasMore;
    private string     _statusMessage     = string.Empty;
    private AuditRowDto? _selectedRow;
    private ObservableCollection<AuditRowDto> _logs = [];
    private int        _offset;

    public AuditViewModel()
    {
        ApplyCommand        = new RelayCommand(async () => await LoadAuditAsync());
        LoadMoreCommand     = new RelayCommand(async () => await LoadMoreAsync(), () => HasMore && !IsLoading);
        ToggleExpandCommand = new RelayCommand<long>(logId =>
        {
            var row = Logs.FirstOrDefault(r => r.LogID == logId);
            if (row is not null) row.IsExpanded = !row.IsExpanded;
        });
    }

    // ── Свойства ─────────────────────────────────────────────────────────

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public string SelectedTable
    {
        get => _selectedTable;
        set => SetProperty(ref _selectedTable, value);
    }

    public string SelectedOperation
    {
        get => _selectedOperation;
        set => SetProperty(ref _selectedOperation, value);
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

    public bool HasMore
    {
        get => _hasMore;
        private set { if (SetProperty(ref _hasMore, value)) CommandManager.InvalidateRequerySuggested(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public AuditRowDto? SelectedRow
    {
        get => _selectedRow;
        set => SetProperty(ref _selectedRow, value);
    }

    public ObservableCollection<AuditRowDto> Logs
    {
        get => _logs;
        private set => SetProperty(ref _logs, value);
    }

    // ── Команды ──────────────────────────────────────────────────────────

    public ICommand ApplyCommand        { get; }
    public ICommand LoadMoreCommand     { get; }
    public ICommand ToggleExpandCommand { get; }

    // ── Загрузка ─────────────────────────────────────────────────────────

    public async Task LoadAuditAsync()
    {
        _offset = 0;
        Logs    = [];
        await FetchPageAsync();
    }

    private async Task LoadMoreAsync()
    {
        _offset += PageSize;
        await FetchPageAsync(append: true);
    }

    private async Task FetchPageAsync(bool append = false)
    {
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            await using var db = new CarCareDbContext();
            var q = db.AuditLogs.AsQueryable();

            var search = SearchText.Trim();
            if (!string.IsNullOrEmpty(search))
                q = q.Where(a => a.ChangedBy != null && a.ChangedBy.Contains(search));

            if (SelectedTable != "Все")
                q = q.Where(a => a.TableName == SelectedTable);

            if (SelectedOperation != "Все")
                q = q.Where(a => a.Operation == SelectedOperation);

            if (DateFrom.HasValue)
                q = q.Where(a => a.ChangedAt >= DateFrom.Value);

            if (DateTo.HasValue)
                q = q.Where(a => a.ChangedAt < DateTo.Value.AddDays(1));

            var raw = await q
                .OrderByDescending(a => a.ChangedAt)
                .Skip(_offset)
                .Take(PageSize)
                .ToListAsync();

            var dtos = raw.Select(a => new AuditRowDto
            {
                LogID           = a.LogID,
                TableName       = a.TableName,
                Operation       = a.Operation,
                PrimaryKeyValue = a.PrimaryKeyValue,
                ChangedBy       = a.ChangedBy,
                ChangedAt       = a.ChangedAt,
                OldValues       = a.OldValues,
                NewValues       = a.NewValues,
            });

            if (append)
                foreach (var dto in dtos) Logs.Add(dto);
            else
                Logs = new ObservableCollection<AuditRowDto>(dtos);

            HasMore = raw.Count == PageSize;
            StatusMessage = $"Записей: {Logs.Count}"
                + (HasMore ? " (есть ещё — нажмите «Загрузить ещё»)" : string.Empty);
        }
        catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
        finally { IsLoading = false; }
    }
}
