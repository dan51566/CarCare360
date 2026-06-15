using CarCare360.Desktop.Data;
using CarCare360.Desktop.Helpers;
using CarCare360.Desktop.Views;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CarCare360.Desktop.ViewModels;

/// <summary>DTO строки механика для DataGrid.</summary>
public record MechanicRowDto(
    int       MechanicID,
    int       UserID,
    string    FullName,
    string    Login,
    string?   Specialization,
    DateTime? HireDate,
    string?   QualificationLevel,
    int       AssignedOrdersCount,
    bool      IsActive
);

/// <summary>DTO заказа для детальной панели механика.</summary>
public record MechanicOrderDto(
    int       OrderID,
    string    ClientName,
    string    CarInfo,
    string    Status,
    DateTime? ScheduledDate
);

public record PendingMechanicDto(int MechanicID, int UserID, string FullName, string Login, DateTime? RegisteredAt);

/// <summary>
/// ViewModel раздела «Механики».
/// Поиск по ФИО / логину, добавление и редактирование, inline-команды, детальная панель заказов.
/// </summary>
public sealed class MechanicsViewModel : BaseViewModel
{
    /// <summary>True для роли «Механик» — скрывает CRUD-кнопки в XAML.</summary>
    public bool IsReadOnly => string.Equals(CurrentUser.RoleName, "Механик", StringComparison.OrdinalIgnoreCase);

    private string  _searchText    = string.Empty;
    private bool    _showFired;
    private bool    _isLoading;
    private string  _statusMessage = string.Empty;
    private ObservableCollection<MechanicRowDto>  _mechanics              = new();
    private ObservableCollection<MechanicOrderDto> _selectedMechanicOrders = new();
    private MechanicRowDto? _selectedMechanic;
    private ObservableCollection<PendingMechanicDto> _pendingMechanics = new();

    public MechanicsViewModel()
    {
        RefreshCommand              = new RelayCommand(async () => await LoadMechanicsAsync());
        AddCommand                  = new RelayCommand(ExecuteAdd);
        EditCommand                 = new RelayCommand(ExecuteEdit, () => SelectedMechanic is not null);
        InlineEditCommand           = new RelayCommand<MechanicRowDto>(ExecuteInlineEdit);
        InlineToggleActiveCommand   = new RelayCommand<MechanicRowDto>(async r => await ExecuteToggleActiveAsync(r));
        ConfirmMechanicCommand = new RelayCommand<PendingMechanicDto>(async r => await ConfirmMechanicAsync(r));
        RejectMechanicCommand  = new RelayCommand<PendingMechanicDto>(async r => await RejectMechanicAsync(r));
    }

    // ── Свойства ─────────────────────────────────────────────────────────

    public string SearchText
    {
        get => _searchText;
        set { if (SetProperty(ref _searchText, value)) _ = LoadMechanicsAsync(); }
    }

    public bool ShowFired
    {
        get => _showFired;
        set { if (SetProperty(ref _showFired, value)) _ = LoadMechanicsAsync(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ObservableCollection<MechanicRowDto> Mechanics
    {
        get => _mechanics;
        private set => SetProperty(ref _mechanics, value);
    }

    public ObservableCollection<MechanicOrderDto> SelectedMechanicOrders
    {
        get => _selectedMechanicOrders;
        private set => SetProperty(ref _selectedMechanicOrders, value);
    }

    public MechanicRowDto? SelectedMechanic
    {
        get => _selectedMechanic;
        set
        {
            if (SetProperty(ref _selectedMechanic, value))
            {
                OnPropertyChanged(nameof(HasSelectedMechanic));
                CommandManager.InvalidateRequerySuggested();
                if (value is not null)
                    _ = LoadSelectedMechanicOrdersAsync(value);
                else
                    SelectedMechanicOrders = new ObservableCollection<MechanicOrderDto>();
            }
        }
    }

    public ObservableCollection<PendingMechanicDto> PendingMechanics
    {
        get => _pendingMechanics;
        private set { SetProperty(ref _pendingMechanics, value); OnPropertyChanged(nameof(HasPendingMechanics)); }
    }
    public bool HasPendingMechanics => _pendingMechanics.Count > 0;

    /// <summary>True когда выбран механик — управляет видимостью детальной панели.</summary>
    public bool HasSelectedMechanic => _selectedMechanic is not null;

    // ── Команды ──────────────────────────────────────────────────────────

    public ICommand RefreshCommand            { get; }
    public ICommand AddCommand                { get; }
    public ICommand EditCommand               { get; }
    public ICommand InlineEditCommand         { get; }
    public ICommand InlineToggleActiveCommand { get; }
    public ICommand ConfirmMechanicCommand    { get; }
    public ICommand RejectMechanicCommand     { get; }

    // ── Загрузка ─────────────────────────────────────────────────────────

    public async Task LoadMechanicsAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            await using var db = new CarCareDbContext();
            var search = SearchText.Trim();

            var raw = await db.Mechanics
                .Include(m => m.User)
                .Include(m => m.Specialization)
                .Where(m => m.HireDate != null)   // только нанятые (не на рассмотрении)
                .Where(m => ShowFired
                    ? m.User!.IsActive == false    // уволенные
                    : m.User!.IsActive == true)    // активные (по умолчанию)
                .Where(m => string.IsNullOrEmpty(search) ||
                    m.User!.FullName.Contains(search) ||
                    m.User!.Login.Contains(search))
                .OrderBy(m => m.User!.FullName)
                .ToListAsync();

            // Баг 3 — был m.UserID, но OrderServices.MechanicID ссылается на Mechanics.MechanicID,
            // а не на Users.UserID. Словарь counts строится с ключами MechanicID, поэтому
            // и выборка, и поиск должны использовать m.MechanicID.
            var mechanicIds = raw.Select(m => m.MechanicID).ToList();
            var counts = await db.OrderServices
                .Where(os => os.MechanicID != null && mechanicIds.Contains(os.MechanicID!.Value))
                .GroupBy(os => os.MechanicID!.Value)
                .Select(g => new { MechanicID = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.MechanicID, x => x.Count);

            Mechanics = new ObservableCollection<MechanicRowDto>(raw.Select(m => new MechanicRowDto(
                m.MechanicID,
                m.UserID,
                m.User?.FullName ?? "—",
                m.User?.Login ?? "—",
                m.Specialization?.Name,
                m.HireDate,
                m.QualificationLevel,
                counts.GetValueOrDefault(m.MechanicID, 0),   // Баг 3 — исправлено с UserID
                m.User?.IsActive ?? false)));

            var pending = await db.Mechanics
                .Include(m => m.User)
                .Where(m => m.HireDate == null && m.User != null && m.User.IsActive == false)
                .OrderBy(m => m.User!.CreatedAt)
                .ToListAsync();
            PendingMechanics = new ObservableCollection<PendingMechanicDto>(
                pending.Select(m => new PendingMechanicDto(
                    m.MechanicID, m.UserID,
                    m.User?.FullName ?? "—",
                    m.User?.Login ?? "—",
                    m.User?.CreatedAt)));

            StatusMessage = $"Механиков: {Mechanics.Count}";
        }
        catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private async Task LoadSelectedMechanicOrdersAsync(MechanicRowDto mechanic)
    {
        try
        {
            await using var db = new CarCareDbContext();

            // Баг 3 (продолжение) — было mechanic.UserID, но FK OrderServices.MechanicID
            // ссылается на Mechanics.MechanicID, а не на Users.UserID. Используем MechanicID,
            // иначе детальная панель показывает заказы не того механика (или пусто).
            var orderIds = await db.OrderServices
                .Where(os => os.MechanicID == mechanic.MechanicID)
                .Select(os => os.OrderID)
                .Distinct()
                .ToListAsync();

            var orders = await db.Orders
                .Include(o => o.Client)
                .Include(o => o.Car).ThenInclude(c => c!.Model).ThenInclude(m => m!.Brand)
                .Where(o => orderIds.Contains(o.OrderID) && o.IsDeleted != true)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            SelectedMechanicOrders = new ObservableCollection<MechanicOrderDto>(
                orders.Select(o => new MechanicOrderDto(
                    o.OrderID,
                    o.Client?.FullName ?? "—",
                    $"{o.Car?.Model?.Brand?.Name} {o.Car?.Model?.Name} • {o.Car?.LicensePlate}",
                    o.Status ?? "Новый",
                    o.ScheduledDate)));
        }
        catch { /* игнорируем ошибки детальной панели */ }
    }

    // ── Действия ─────────────────────────────────────────────────────────

    private void ExecuteAdd()
    {
        var vm  = new MechanicEditViewModel(onSaved: async () => await LoadMechanicsAsync());
        var dlg = new MechanicEditDialog { DataContext = vm };
        vm.CloseRequested += (_, _) => dlg.Close();
        DialogHelper.SetOwner(dlg);
        dlg.ShowDialog();
        ToastHelper.Show("Механик добавлен", ToastType.Success);
    }

    private void ExecuteEdit()
    {
        if (SelectedMechanic is null) return;
        OpenEditDialog(SelectedMechanic);
    }

    private void ExecuteInlineEdit(MechanicRowDto? row)
    {
        if (row is null) return;
        OpenEditDialog(row);
    }

    private void OpenEditDialog(MechanicRowDto row)
    {
        var vm  = new MechanicEditViewModel(row, onSaved: async () => await LoadMechanicsAsync());
        var dlg = new MechanicEditDialog { DataContext = vm };
        vm.CloseRequested += (_, _) => dlg.Close();
        DialogHelper.SetOwner(dlg);
        dlg.ShowDialog();
    }

    private async Task ExecuteToggleActiveAsync(MechanicRowDto? row)
    {
        if (row is null) return;
        try
        {
            await using var db = new CarCareDbContext();
            var user = await db.Users.FindAsync(row.UserID);
            if (user is null) return;
            user.IsActive = !(user.IsActive ?? false);
            await db.SaveChangesAsync();
            await LoadMechanicsAsync();
            var state = user.IsActive == true ? "активирован" : "деактивирован";
            ToastHelper.Show($"Механик «{row.FullName}» {state}", ToastType.Success);
        }
        catch (Exception ex)
        {
            ToastHelper.Show($"Ошибка: {ex.Message}", ToastType.Error);
        }
    }

    private async Task ConfirmMechanicAsync(PendingMechanicDto? row)
    {
        if (row is null) return;
        try
        {
            await using var db = new CarCareDbContext();
            var user = await db.Users.FindAsync(row.UserID);
            var mech = await db.Mechanics.FindAsync(row.MechanicID);
            if (user is null || mech is null) return;
            user.IsActive  = true;
            mech.HireDate  = DateTime.Today;
            await db.SaveChangesAsync();
            await LoadMechanicsAsync();
            ToastHelper.Show($"Механик «{row.FullName}» подтверждён", ToastType.Success);
        }
        catch (Exception ex) { ToastHelper.Show($"Ошибка: {ex.Message}", ToastType.Error); }
    }

    private async Task RejectMechanicAsync(PendingMechanicDto? row)
    {
        if (row is null) return;
        try
        {
            await using var db = new CarCareDbContext();
            var mech = await db.Mechanics.FindAsync(row.MechanicID);
            if (mech is not null) db.Mechanics.Remove(mech);
            var user = await db.Users.FindAsync(row.UserID);
            if (user is not null) db.Users.Remove(user);
            await db.SaveChangesAsync();
            await LoadMechanicsAsync();
            ToastHelper.Show($"Заявка «{row.FullName}» отклонена", ToastType.Warning);
        }
        catch (Exception ex) { ToastHelper.Show($"Ошибка: {ex.Message}", ToastType.Error); }
    }
}
