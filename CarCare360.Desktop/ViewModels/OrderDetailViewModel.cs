using CarCare360.Desktop.Data;
using CarCare360.Desktop.Helpers;
using CarCare360.Desktop.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows.Input;

namespace CarCare360.Desktop.ViewModels;

// ── Вспомогательные DTO для детали заказа ────────────────────────────────

/// <summary>DTO одной строки услуги в заказе (для DataGrid).</summary>
public record ServiceLineDto(
    int      OrderServiceID,
    string   ServiceName,
    string?  MechanicName,
    string?  BayName,
    string?  LineStatus,
    decimal  Price,
    decimal  NormHour
);

/// <summary>DTO одной строки запчасти в заказе (для DataGrid).</summary>
public record PartLineDto(
    int      OrderPartID,
    string   PartName,
    int      Quantity,
    decimal  PricePerUnit,
    decimal  Total
);

/// <summary>Элемент списка услуг для выбора при добавлении.</summary>
public record ServiceItem(int ServiceID, string Name, decimal BasePrice);

/// <summary>Элемент списка механиков для назначения.</summary>
public record MechanicItem(int MechanicID, string FullName);

/// <summary>Элемент списка боксов для назначения.</summary>
public record BayItem(int BayID, string Name);

/// <summary>Элемент списка запчастей для добавления в заказ.</summary>
public record PartItem(int PartID, string Name, int Stock, decimal Price);

/// <summary>
/// ViewModel диалога детали заказ-наряда.
/// Показывает услуги и запчасти, позволяет менять статус,
/// добавлять/удалять позиции, редактировать примечания и время.
/// </summary>
public sealed class OrderDetailViewModel : BaseViewModel
{
    private readonly int        _orderId;
    private readonly Func<Task> _onChanged;

    // ── Поля заголовка ────────────────────────────────────────────────────
    private string    _clientName    = string.Empty;
    private string    _carInfo       = string.Empty;
    private string    _currentStatus = string.Empty;
    private DateTime? _createdAt;
    private string    _scheduledDate = string.Empty;
    private string    _scheduledTime = string.Empty;
    private string    _notes         = string.Empty;

    // ── Состояние UI ─────────────────────────────────────────────────────
    private bool   _isBusy;
    private string _errorMessage  = string.Empty;
    private bool   _isAddingService;
    private bool   _isAddingPart;

    // ── Поля добавления услуги ────────────────────────────────────────────
    private ServiceItem?  _selectedNewService;
    private MechanicItem? _selectedNewMechanic;
    private BayItem?      _selectedNewBay;

    // ── Поля добавления запчасти ──────────────────────────────────────────
    private PartItem? _selectedNewPart;
    private int       _newPartQty = 1;

    // ── Выбранные строки DataGrid ─────────────────────────────────────────
    private ServiceLineDto? _selectedServiceLine;
    private PartLineDto?    _selectedPartLine;

    /// <summary>Допустимые статусы заказа (для ComboBox смены статуса).</summary>
    public static readonly string[] EditableStatuses =
        ["Новый", "Назначен", "В работе", "Ожидает запчасти", "Готов", "Выдан", "Отменён"];

    // Баг 2 — финальные статусы, при которых редактирование заблокировано.
    private static readonly HashSet<string> _finalStatuses =
        new(StringComparer.Ordinal) { "Готов", "Выдан", "Отменён" };

    /// <summary>
    /// true — заказ в финальном статусе, редактирование недоступно.
    /// Привязывается к баннеру и IsEnabled кнопок в диалоге.
    /// </summary>
    public bool IsReadOnly => _finalStatuses.Contains(_currentStatus);

    public OrderDetailViewModel(int orderId, Func<Task> onChanged)
    {
        _orderId   = orderId;
        _onChanged = onChanged;

        // Основные команды
        RefreshCommand      = new RelayCommand(async () => await LoadAsync());
        // Баг 2 — запрещаем смену статуса и редактирование шапки для финальных статусов.
        ChangeStatusCommand = new RelayCommand(async () => await ChangeStatusAsync(),
                                  () => !IsBusy && !IsReadOnly);
        SaveHeaderCommand   = new RelayCommand(async () => await SaveHeaderAsync(),
                                  () => !IsBusy && !IsReadOnly);
        CloseCommand        = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));

        // Команды добавления услуги
        ShowAddServiceCommand    = new RelayCommand(() => { IsAddingService = true; IsAddingPart = false; });
        CancelAddServiceCommand  = new RelayCommand(() => { IsAddingService = false; SelectedNewService = null; });
        ConfirmAddServiceCommand = new RelayCommand(async () => await ConfirmAddServiceAsync(),
                                      () => SelectedNewService is not null && !IsBusy);

        // Команды добавления запчасти
        ShowAddPartCommand    = new RelayCommand(() => { IsAddingPart = true; IsAddingService = false; });
        CancelAddPartCommand  = new RelayCommand(() => { IsAddingPart = false; SelectedNewPart = null; NewPartQty = 1; });
        ConfirmAddPartCommand = new RelayCommand(async () => await ConfirmAddPartAsync(),
                                    () => SelectedNewPart is not null && NewPartQty > 0 && !IsBusy);

        // Удаление строк
        RemoveServiceCommand = new RelayCommand(async () => await RemoveServiceAsync(),
                                   () => SelectedServiceLine is not null && !IsBusy);
        RemovePartCommand    = new RelayCommand(async () => await RemovePartAsync(),
                                   () => SelectedPartLine is not null && !IsBusy);

        _ = LoadAsync();
    }

    // ── Свойства заголовка ────────────────────────────────────────────────

    public int OrderId => _orderId;

    public string ClientName
    {
        get => _clientName;
        private set => SetProperty(ref _clientName, value);
    }

    public string CarInfo
    {
        get => _carInfo;
        private set => SetProperty(ref _carInfo, value);
    }

    public string CurrentStatus
    {
        get => _currentStatus;
        set
        {
            SetProperty(ref _currentStatus, value);
            // Баг 2 — при смене статуса обновляем IsReadOnly и CanExecute команд.
            OnPropertyChanged(nameof(IsReadOnly));
            RaiseCanExecute();
        }
    }

    public DateTime? CreatedAt
    {
        get => _createdAt;
        private set => SetProperty(ref _createdAt, value);
    }

    public string ScheduledDate
    {
        get => _scheduledDate;
        set => SetProperty(ref _scheduledDate, value);
    }

    public string ScheduledTime
    {
        get => _scheduledTime;
        set => SetProperty(ref _scheduledTime, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    // ── Состояние UI ─────────────────────────────────────────────────────

    public bool IsBusy
    {
        get => _isBusy;
        set { SetProperty(ref _isBusy, value); RaiseCanExecute(); }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsAddingService
    {
        get => _isAddingService;
        set => SetProperty(ref _isAddingService, value);
    }

    public bool IsAddingPart
    {
        get => _isAddingPart;
        set => SetProperty(ref _isAddingPart, value);
    }

    // ── Коллекции ─────────────────────────────────────────────────────────

    public ObservableCollection<ServiceLineDto> ServiceLines     { get; } = new();
    public ObservableCollection<PartLineDto>    PartLines        { get; } = new();
    public ObservableCollection<ServiceItem>    AvailableServices  { get; } = new();
    public ObservableCollection<MechanicItem>   AvailableMechanics { get; } = new();
    public ObservableCollection<BayItem>        AvailableBays      { get; } = new();
    public ObservableCollection<PartItem>       AvailableParts     { get; } = new();

    // ── Выбор в панелях добавления ────────────────────────────────────────

    public ServiceItem? SelectedNewService
    {
        get => _selectedNewService;
        set { SetProperty(ref _selectedNewService, value); RaiseCanExecute(); }
    }

    public MechanicItem? SelectedNewMechanic
    {
        get => _selectedNewMechanic;
        set => SetProperty(ref _selectedNewMechanic, value);
    }

    public BayItem? SelectedNewBay
    {
        get => _selectedNewBay;
        set => SetProperty(ref _selectedNewBay, value);
    }

    public PartItem? SelectedNewPart
    {
        get => _selectedNewPart;
        set { SetProperty(ref _selectedNewPart, value); RaiseCanExecute(); }
    }

    public int NewPartQty
    {
        get => _newPartQty;
        set { SetProperty(ref _newPartQty, value < 1 ? 1 : value); RaiseCanExecute(); }
    }

    // ── Выбранные строки ──────────────────────────────────────────────────

    public ServiceLineDto? SelectedServiceLine
    {
        get => _selectedServiceLine;
        set { SetProperty(ref _selectedServiceLine, value); RaiseCanExecute(); }
    }

    public PartLineDto? SelectedPartLine
    {
        get => _selectedPartLine;
        set { SetProperty(ref _selectedPartLine, value); RaiseCanExecute(); }
    }

    // ── Итоги (вычисляемые) ───────────────────────────────────────────────

    public decimal ServicesTotalCost => ServiceLines.Sum(s => s.Price);
    public decimal PartsTotalCost    => PartLines.Sum(p => p.Total);
    public decimal GrandTotal        => ServicesTotalCost + PartsTotalCost;

    // ── Команды ──────────────────────────────────────────────────────────

    public ICommand RefreshCommand           { get; }
    public ICommand ChangeStatusCommand      { get; }
    public ICommand SaveHeaderCommand        { get; }
    public ICommand ShowAddServiceCommand    { get; }
    public ICommand CancelAddServiceCommand  { get; }
    public ICommand ConfirmAddServiceCommand { get; }
    public ICommand ShowAddPartCommand       { get; }
    public ICommand CancelAddPartCommand     { get; }
    public ICommand ConfirmAddPartCommand    { get; }
    public ICommand RemoveServiceCommand     { get; }
    public ICommand RemovePartCommand        { get; }
    public ICommand CloseCommand             { get; }

    public event EventHandler? CloseRequested;

    // ── Загрузка данных ───────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            await using var db = new CarCareDbContext();
            var order = await db.Orders
                .Include(o => o.Client)
                .Include(o => o.Car).ThenInclude(c => c!.Model).ThenInclude(m => m!.Brand)
                .Include(o => o.OrderServices).ThenInclude(os => os.Service)
                .Include(o => o.OrderServices).ThenInclude(os => os.Bay)
                .Include(o => o.OrderParts).ThenInclude(op => op.Part)
                .FirstOrDefaultAsync(o => o.OrderID == _orderId);

            if (order is null) { ErrorMessage = "Заказ не найден."; return; }

            // Словарь MechanicID → FullName для корректного отображения имён.
            // FK в БД: OrderServices.MechanicID → Mechanics.MechanicID (не Users.UserID),
            // поэтому навигация через os.Mechanic (User) даёт неверные результаты.
            var mechanicNames = await db.Mechanics
                .Include(m => m.User)
                .ToDictionaryAsync(m => m.MechanicID, m => m.User?.FullName ?? "—");

            // Обновляем заголовок
            ClientName    = order.Client?.FullName ?? "—";
            CarInfo       = $"{order.Car?.Model?.Brand?.Name} {order.Car?.Model?.Name} • {order.Car?.LicensePlate}";
            CurrentStatus = order.Status ?? "Новый";
            CreatedAt     = order.CreatedAt;
            ScheduledDate = order.ScheduledDate?.ToString("dd.MM.yyyy") ?? string.Empty;
            ScheduledTime = order.ScheduledTime.HasValue
                ? $"{(int)order.ScheduledTime.Value.TotalHours:D2}:{order.ScheduledTime.Value.Minutes:D2}"
                : string.Empty;
            Notes = order.Notes ?? string.Empty;

            // Услуги
            ServiceLines.Clear();
            foreach (var os in order.OrderServices)
                ServiceLines.Add(new ServiceLineDto(
                    os.OrderServiceID,
                    os.Service?.Name ?? "—",
                    os.MechanicID.HasValue
                        ? mechanicNames.GetValueOrDefault(os.MechanicID.Value)
                        : null,
                    os.Bay?.Name,
                    os.Status,
                    os.Service?.BasePrice ?? 0m,
                    os.Service?.NormHour  ?? 0m));

            // Запчасти
            PartLines.Clear();
            foreach (var op in order.OrderParts)
                PartLines.Add(new PartLineDto(
                    op.OrderPartID,
                    op.Part?.Name ?? "—",
                    op.Quantity,
                    op.PricePerUnit ?? 0m,
                    (op.PricePerUnit ?? 0m) * op.Quantity));

            // Справочники для добавления
            await LoadLookupsAsync(db);
            NotifyTotals();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }

    /// <summary>Загружает справочники (услуги, механики, боксы, запчасти) для панелей добавления.</summary>
    private async Task LoadLookupsAsync(CarCareDbContext db)
    {
        // Список услуг
        AvailableServices.Clear();
        var services = await db.Services.OrderBy(s => s.Name).ToListAsync();
        foreach (var s in services)
            AvailableServices.Add(new ServiceItem(s.ServiceID, s.Name, s.BasePrice ?? 0m));

        // Список механиков — нужен MechanicID из таблицы Mechanics (не UserID из Users),
        // т.к. FK_OS_Mechanic ссылается на Mechanics.MechanicID
        AvailableMechanics.Clear();
        AvailableMechanics.Add(new MechanicItem(0, "— не назначен —"));
        var mechanics = await db.Mechanics
            .Include(m => m.User)
            .Where(m => m.User != null && m.User.IsActive == true)
            .OrderBy(m => m.User!.FullName)
            .ToListAsync();
        foreach (var m in mechanics)
            AvailableMechanics.Add(new MechanicItem(m.MechanicID, m.User?.FullName ?? "—"));
        if (SelectedNewMechanic is null)
            SelectedNewMechanic = AvailableMechanics[0];

        // Список боксов
        AvailableBays.Clear();
        AvailableBays.Add(new BayItem(0, "— не назначен —"));
        var bays = await db.ServiceBays
            .Where(b => b.IsActive == true)
            .OrderBy(b => b.Name)
            .ToListAsync();
        foreach (var b in bays)
            AvailableBays.Add(new BayItem(b.BayID, b.Name));
        if (SelectedNewBay is null)
            SelectedNewBay = AvailableBays[0];

        // Запчасти с остатком > 0
        AvailableParts.Clear();
        var parts = await db.Parts
            .Where(p => p.QuantityInStock > 0)
            .OrderBy(p => p.Name)
            .ToListAsync();
        foreach (var p in parts)
            AvailableParts.Add(new PartItem(p.PartID, p.Name, p.QuantityInStock ?? 0, p.Price ?? 0m));
    }

    // ── Смена статуса ─────────────────────────────────────────────────────

    private async Task ChangeStatusAsync()
    {
        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            await using var db = new CarCareDbContext();
            var cs = db.Database.GetConnectionString()!;
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("UpdateOrderStatus", conn)
                { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@OrderID",   _orderId);
            cmd.Parameters.AddWithValue("@NewStatus", CurrentStatus);
            await cmd.ExecuteNonQueryAsync();
            await _onChanged();
        }
        catch (Exception ex) { ErrorMessage = ex.InnerException?.Message ?? ex.Message; }
        finally { IsBusy = false; }
    }

    // ── Сохранение заголовка (дата, время, примечания) ────────────────────

    private async Task SaveHeaderAsync()
    {
        ErrorMessage = string.Empty;

        // Парсинг даты
        DateTime? scheduledDate = null;
        if (!string.IsNullOrWhiteSpace(ScheduledDate))
        {
            if (!DateTime.TryParseExact(ScheduledDate.Trim(),
                    ["dd.MM.yyyy", "yyyy-MM-dd", "d.MM.yyyy"],
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var d))
            { ErrorMessage = "Неверный формат даты. Используйте ДД.ММ.ГГГГ."; return; }
            scheduledDate = d;
        }

        // Парсинг времени
        TimeSpan? scheduledTime = null;
        if (!string.IsNullOrWhiteSpace(ScheduledTime))
        {
            if (!TimeSpan.TryParseExact(ScheduledTime.Trim(), ["hh\\:mm", "h\\:mm"],
                    System.Globalization.CultureInfo.InvariantCulture, out var t))
            { ErrorMessage = "Неверный формат времени. Используйте ЧЧ:ММ."; return; }
            scheduledTime = t;
        }

        IsBusy = true;
        try
        {
            await using var db = new CarCareDbContext();
            var order = await db.Orders.FindAsync(_orderId);
            if (order is null) return;
            order.Notes         = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();
            order.ScheduledDate = scheduledDate;
            order.ScheduledTime = scheduledTime;
            await db.SaveChangesAsync();
            await _onChanged();
        }
        catch (Exception ex) { ErrorMessage = ex.InnerException?.Message ?? ex.Message; }
        finally { IsBusy = false; }
    }

    // ── Добавление услуги ─────────────────────────────────────────────────

    private async Task ConfirmAddServiceAsync()
    {
        if (SelectedNewService is null) return;
        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            await using var db = new CarCareDbContext();
            var cs = db.Database.GetConnectionString()!;
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("AddServiceToOrder", conn)
                { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@OrderID",   _orderId);
            cmd.Parameters.AddWithValue("@ServiceID", SelectedNewService.ServiceID);
            cmd.Parameters.AddWithValue("@MechanicID",
                SelectedNewMechanic?.MechanicID > 0 ? (object)SelectedNewMechanic.MechanicID : DBNull.Value);
            cmd.Parameters.AddWithValue("@BayID",
                SelectedNewBay?.BayID > 0 ? (object)SelectedNewBay.BayID : DBNull.Value);
            await cmd.ExecuteNonQueryAsync();

            IsAddingService    = false;
            SelectedNewService = null;
            await _onChanged();
        }
        catch (Exception ex) { ErrorMessage = ex.InnerException?.Message ?? ex.Message; IsBusy = false; return; }
        finally { IsBusy = false; }

        await LoadAsync();
    }

    // ── Добавление запчасти ───────────────────────────────────────────────

    private async Task ConfirmAddPartAsync()
    {
        if (SelectedNewPart is null || NewPartQty <= 0) return;
        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            await using var db = new CarCareDbContext();
            var cs = db.Database.GetConnectionString()!;
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("AddPartToOrder", conn)
                { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@OrderID",  _orderId);
            cmd.Parameters.AddWithValue("@PartID",   SelectedNewPart.PartID);
            cmd.Parameters.AddWithValue("@Quantity", NewPartQty);
            await cmd.ExecuteNonQueryAsync();

            IsAddingPart    = false;
            SelectedNewPart = null;
            NewPartQty      = 1;
            await _onChanged();
        }
        catch (Exception ex) { ErrorMessage = ex.InnerException?.Message ?? ex.Message; IsBusy = false; return; }
        finally { IsBusy = false; }

        await LoadAsync();
    }

    // ── Удаление услуги ───────────────────────────────────────────────────

    private async Task RemoveServiceAsync()
    {
        if (SelectedServiceLine is null) return;
        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            await using var db = new CarCareDbContext();
            var entity = await db.OrderServices.FindAsync(SelectedServiceLine.OrderServiceID);
            if (entity is not null)
            {
                db.OrderServices.Remove(entity);
                await db.SaveChangesAsync();
            }
            await _onChanged();
        }
        catch (Exception ex) { ErrorMessage = ex.InnerException?.Message ?? ex.Message; IsBusy = false; return; }
        finally { IsBusy = false; }

        await LoadAsync();
    }

    // ── Удаление запчасти (с возвратом на склад) ──────────────────────────

    private async Task RemovePartAsync()
    {
        if (SelectedPartLine is null) return;
        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            await using var db = new CarCareDbContext();
            var entity = await db.OrderParts
                .Include(p => p.Part)
                .FirstOrDefaultAsync(p => p.OrderPartID == SelectedPartLine.OrderPartID);

            if (entity is not null)
            {
                // Возвращаем количество на склад
                if (entity.Part is not null)
                    entity.Part.QuantityInStock = (entity.Part.QuantityInStock ?? 0) + entity.Quantity;

                db.OrderParts.Remove(entity);
                await db.SaveChangesAsync();
            }
            await _onChanged();
        }
        catch (Exception ex) { ErrorMessage = ex.InnerException?.Message ?? ex.Message; IsBusy = false; return; }
        finally { IsBusy = false; }

        await LoadAsync();
    }

    // ── Вспомогательные ──────────────────────────────────────────────────

    private void RaiseCanExecute() => CommandManager.InvalidateRequerySuggested();

    private void NotifyTotals()
    {
        OnPropertyChanged(nameof(ServicesTotalCost));
        OnPropertyChanged(nameof(PartsTotalCost));
        OnPropertyChanged(nameof(GrandTotal));
        RecalcScheduledTime();
    }

    /// <summary>
    /// Пересчитывает плановое время работы:
    /// суммирует нормо-часы всех услуг и умножает на 1.8.
    /// Результат записывается в ScheduledTime в формате ЧЧ:ММ.
    /// </summary>
    private void RecalcScheduledTime()
    {
        var totalMinutes = (double)ServiceLines.Sum(s => s.NormHour) * 1.8 * 60;
        if (totalMinutes <= 0)
        {
            ScheduledTime = string.Empty;
            return;
        }
        var hours   = (int)totalMinutes / 60;
        var minutes = (int)totalMinutes % 60;
        ScheduledTime = $"{hours:D2}:{minutes:D2}";
    }
}
