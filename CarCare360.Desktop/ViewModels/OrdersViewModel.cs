using CarCare360.Desktop.Data;
using CarCare360.Desktop.Helpers;
using CarCare360.Desktop.Views;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CarCare360.Desktop.ViewModels;

/// <summary>DTO строки заказа для карточки/списка.</summary>
public record OrderRowDto(
    int       OrderID,
    string    Status,
    string    ClientFullName,
    string    CarInfo,         // «Toyota Camry • А123БВ78»
    DateTime? CreatedAt,
    DateTime? ScheduledDate,
    int       ServiceCount,
    int       PartCount,
    decimal   TotalCost,
    string?   Notes,
    bool      IsDeleted
);

/// <summary>
/// ViewModel списка заказов.
/// Поиск по клиенту / авто / ID, фильтр по статусу (чип-кнопки) и мягкому удалению.
/// </summary>
public sealed class OrdersViewModel : BaseViewModel
{
    public static readonly string[] AllStatuses =
        ["Все", "Новый", "В работе", "Ожидает запчасти", "Готов", "Выдан", "Отменён"];

    private string _searchText    = string.Empty;
    private string _statusFilter  = "Все";
    private bool   _showDeleted;
    private bool   _isLoading;
    private string _statusMessage = string.Empty;
    private ObservableCollection<OrderRowDto> _orders = new();
    private OrderRowDto? _selectedOrder;

    public OrdersViewModel()
    {
        RefreshCommand        = new RelayCommand(async () => await LoadOrdersAsync());
        CreateCommand         = new RelayCommand(ExecuteCreate);
        OpenCommand           = new RelayCommand(ExecuteOpen, () => SelectedOrder is not null);
        DeleteCommand         = new RelayCommand(async () => await ExecuteDeleteAsync(),
                                    () => SelectedOrder is { IsDeleted: false });
        SetStatusFilterCommand = new RelayCommand<string>(s => StatusFilter = s ?? "Все");
        OpenCardCommand       = new RelayCommand<OrderRowDto>(ExecuteOpenCard);
        DeleteCardCommand     = new RelayCommand<OrderRowDto>(async r => await ExecuteDeleteCardAsync(r));
    }

    // ── Свойства ──────────────────────────────────────────────────────────

    public string SearchText
    {
        get => _searchText;
        set { if (SetProperty(ref _searchText, value)) _ = LoadOrdersAsync(); }
    }

    public string StatusFilter
    {
        get => _statusFilter;
        set { if (SetProperty(ref _statusFilter, value)) _ = LoadOrdersAsync(); }
    }

    public bool ShowDeleted
    {
        get => _showDeleted;
        set { if (SetProperty(ref _showDeleted, value)) _ = LoadOrdersAsync(); }
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

    public ObservableCollection<OrderRowDto> Orders
    {
        get => _orders;
        private set => SetProperty(ref _orders, value);
    }

    public OrderRowDto? SelectedOrder
    {
        get => _selectedOrder;
        set { SetProperty(ref _selectedOrder, value); CommandManager.InvalidateRequerySuggested(); }
    }

    // ── Команды ──────────────────────────────────────────────────────────

    public ICommand RefreshCommand         { get; }
    public ICommand CreateCommand          { get; }
    public ICommand OpenCommand            { get; }
    public ICommand DeleteCommand          { get; }
    public ICommand SetStatusFilterCommand { get; }
    public ICommand OpenCardCommand        { get; }
    public ICommand DeleteCardCommand      { get; }

    // ── Загрузка ─────────────────────────────────────────────────────────

    public async Task LoadOrdersAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            await using var db = new CarCareDbContext();
            var search = SearchText.Trim();

            var raw = await db.Orders
                .Include(o => o.Client)
                .Include(o => o.Car).ThenInclude(c => c!.Model).ThenInclude(m => m!.Brand)
                .Include(o => o.OrderServices).ThenInclude(os => os.Service)
                .Include(o => o.OrderParts)
                .Where(o => !ShowDeleted ? o.IsDeleted != true : true)
                .Where(o => StatusFilter == "Все" || o.Status == StatusFilter)
                .Where(o => string.IsNullOrEmpty(search) ||
                    o.Client!.FullName.Contains(search) ||
                    o.Car!.LicensePlate.Contains(search) ||
                    o.OrderID.ToString().Contains(search))
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var rows = raw.Select(o => new OrderRowDto(
                o.OrderID,
                o.Status ?? "Новый",
                o.Client?.FullName ?? "—",
                $"{o.Car?.Model?.Brand?.Name} {o.Car?.Model?.Name} • {o.Car?.LicensePlate}",
                o.CreatedAt,
                o.ScheduledDate,
                o.OrderServices.Count,
                o.OrderParts.Count,
                o.OrderServices.Sum(os => os.Service?.BasePrice ?? 0m) +
                o.OrderParts.Sum(op => (op.PricePerUnit ?? 0m) * op.Quantity),
                o.Notes,
                o.IsDeleted ?? false)).ToList();

            Orders = new ObservableCollection<OrderRowDto>(rows);

            var total = rows.Sum(r => r.TotalCost);
            StatusMessage = $"Заказов: {rows.Count} | Общая сумма: {total:N0} ₽";
        }
        catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    // ── Действия ─────────────────────────────────────────────────────────

    private void ExecuteCreate()
    {
        var vm = new OrderCreateViewModel(onSaved: async () => await LoadOrdersAsync());
        var dlg = new OrderCreateDialog { DataContext = vm };
        vm.CloseRequested += (_, _) => dlg.Close();
        DialogHelper.SetOwner(dlg);
        dlg.ShowDialog();
    }

    private void ExecuteOpen()
    {
        if (SelectedOrder is null) return;
        OpenOrderById(SelectedOrder.OrderID);
    }

    private void ExecuteOpenCard(OrderRowDto? row)
    {
        if (row is null) return;
        OpenOrderById(row.OrderID);
    }

    private void OpenOrderById(int orderId)
    {
        var vm = new OrderDetailViewModel(orderId, onChanged: async () => await LoadOrdersAsync());
        var dlg = new OrderDetailDialog { DataContext = vm };
        vm.CloseRequested += (_, _) => dlg.Close();
        DialogHelper.SetOwner(dlg);
        dlg.ShowDialog();
    }

    private async Task ExecuteDeleteAsync()
    {
        if (SelectedOrder is null) return;
        await SoftDeleteOrderAsync(SelectedOrder.OrderID);
    }

    private async Task ExecuteDeleteCardAsync(OrderRowDto? row)
    {
        if (row is null || row.IsDeleted) return;
        await SoftDeleteOrderAsync(row.OrderID);
    }

    private async Task SoftDeleteOrderAsync(int orderId)
    {
        try
        {
            await using var db = new CarCareDbContext();
            var o = await db.Orders.FindAsync(orderId);
            if (o is null) return;
            o.IsDeleted = true;
            await db.SaveChangesAsync();
            await LoadOrdersAsync();
            ToastHelper.Show($"Заказ #{orderId} удалён", ToastType.Success);
        }
        catch (Exception ex)
        {
            ToastHelper.Show($"Ошибка удаления: {ex.Message}", ToastType.Error);
        }
    }
}
