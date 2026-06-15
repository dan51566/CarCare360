using CarCare360.Desktop.Data;
using CarCare360.Desktop.Helpers;
using CarCare360.Desktop.Views;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace CarCare360.Desktop.ViewModels;

/// <summary>
/// DTO для отображения одного автомобиля в DataGrid.
/// </summary>
public record CarRowDto(
    int     CarID,
    int     ClientID,
    int     ModelID,
    int     BrandID,
    string  BrandName,
    string  ModelName,
    int?    Year,
    string  LicensePlate,
    string? VIN,
    string? Color,
    int?    Mileage,
    string  ClientFullName
);

/// <summary>
/// ViewModel раздела «Автомобили».
/// Загружает список автомобилей с марками, моделями и владельцами.
/// Поддерживает поиск по гос. номеру, VIN и ФИО владельца.
/// </summary>
public sealed class CarsViewModel : BaseViewModel
{
    private string _searchText = string.Empty;
    private ObservableCollection<CarRowDto> _cars = new();
    private CarRowDto? _selectedCar;
    private bool _isLoading;
    private string _statusMessage = string.Empty;

    // Необязательный фильтр по конкретному клиенту (для вызова из модуля Клиентов)
    private int? _clientIdFilter;

    public CarsViewModel(int? clientIdFilter = null)
    {
        _clientIdFilter = clientIdFilter;

        RefreshCommand = new RelayCommand(async () => await LoadCarsAsync());
        AddCommand     = new RelayCommand(ExecuteAdd);
        EditCommand    = new RelayCommand(ExecuteEdit,    () => SelectedCar is not null);
        DeleteCommand  = new RelayCommand(async () => await ExecuteDeleteAsync(), () => SelectedCar is not null);

        InlineEditCommand   = new RelayCommand<CarRowDto>(ExecuteInlineEdit);
        InlineDeleteCommand = new RelayCommand<CarRowDto>(async r => await ExecuteInlineDeleteAsync(r));
        ViewOwnerCommand    = new RelayCommand<CarRowDto>(ExecuteViewOwner);
    }

    // ── Свойства ──────────────────────────────────────────────────────────

    /// <summary>Строка поиска — гос.номер, VIN или ФИО владельца.</summary>
    public string SearchText
    {
        get => _searchText;
        set { if (SetProperty(ref _searchText, value)) _ = LoadCarsAsync(); }
    }

    public ObservableCollection<CarRowDto> Cars
    {
        get => _cars;
        private set => SetProperty(ref _cars, value);
    }

    public CarRowDto? SelectedCar
    {
        get => _selectedCar;
        set
        {
            SetProperty(ref _selectedCar, value);
            CommandManager.InvalidateRequerySuggested();
        }
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

    // ── Команды ──────────────────────────────────────────────────────────

    public ICommand RefreshCommand     { get; }
    public ICommand AddCommand         { get; }
    public ICommand EditCommand        { get; }
    public ICommand DeleteCommand      { get; }
    public ICommand InlineEditCommand  { get; }
    public ICommand InlineDeleteCommand { get; }
    public ICommand ViewOwnerCommand   { get; }

    // ── Загрузка ─────────────────────────────────────────────────────────

    public async Task LoadCarsAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            await using var db = new CarCareDbContext();
            var search = SearchText.Trim();

            var query = db.Cars
                .Include(c => c.Model)
                    .ThenInclude(m => m!.Brand)
                .Include(c => c.Client)
                .AsQueryable();

            // Опциональный фильтр по владельцу
            if (_clientIdFilter.HasValue)
                query = query.Where(c => c.ClientID == _clientIdFilter.Value);

            // Поиск по гос.номеру, VIN или ФИО клиента
            if (!string.IsNullOrEmpty(search))
                query = query.Where(c =>
                    c.LicensePlate.Contains(search) ||
                    (c.VIN != null && c.VIN.Contains(search)) ||
                    c.Client!.FullName.Contains(search) ||
                    c.Model!.Brand!.Name.Contains(search));

            var rows = await query
                .OrderBy(c => c.Client!.FullName)
                .ThenBy(c => c.LicensePlate)
                .Select(c => new
                {
                    c.CarID, c.ClientID,
                    c.ModelID,
                    BrandID       = c.Model!.BrandID,
                    BrandName     = c.Model!.Brand!.Name,
                    ModelName     = c.Model!.Name,
                    c.Year, c.LicensePlate, c.VIN, c.Color, c.Mileage,
                    ClientName    = c.Client!.FullName
                })
                .ToListAsync();

            Cars = new ObservableCollection<CarRowDto>(
                rows.Select(r => new CarRowDto(
                    r.CarID, r.ClientID, r.ModelID, r.BrandID,
                    r.BrandName, r.ModelName,
                    r.Year, r.LicensePlate, r.VIN, r.Color, r.Mileage,
                    r.ClientName)));

            StatusMessage = $"Автомобилей: {rows.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Действия ─────────────────────────────────────────────────────────

    private void ExecuteAdd()
    {
        var vm = new CarEditViewModel(
            presetClientId: _clientIdFilter,
            onSaved: async () => await LoadCarsAsync());
        var dialog = new CarEditDialog { DataContext = vm };
        vm.CloseRequested += (_, _) => dialog.Close();
        DialogHelper.SetOwner(dialog);
        dialog.ShowDialog();
        ToastHelper.Show("Автомобиль добавлен", ToastType.Success);
    }

    private void ExecuteEdit()
    {
        if (SelectedCar is null) return;
        var vm = new CarEditViewModel(SelectedCar, onSaved: async () => await LoadCarsAsync());
        var dialog = new CarEditDialog { DataContext = vm };
        vm.CloseRequested += (_, _) => dialog.Close();
        DialogHelper.SetOwner(dialog);
        dialog.ShowDialog();
    }

    private void ExecuteInlineEdit(CarRowDto? row)
    {
        if (row is null) return;
        var vm = new CarEditViewModel(row, onSaved: async () => await LoadCarsAsync());
        var dialog = new CarEditDialog { DataContext = vm };
        vm.CloseRequested += (_, _) => dialog.Close();
        DialogHelper.SetOwner(dialog);
        dialog.ShowDialog();
    }

    private async Task ExecuteInlineDeleteAsync(CarRowDto? row)
    {
        if (row is null) return;
        try
        {
            await using var db = new CarCareDbContext();

            bool hasOrders = await db.Orders.AnyAsync(o => o.CarID == row.CarID);
            if (hasOrders)
            {
                ToastHelper.Show($"Нельзя удалить «{row.LicensePlate}»: есть связанные заказы", ToastType.Warning);
                return;
            }

            var car = await db.Cars.FindAsync(row.CarID);
            if (car is null) return;
            db.Cars.Remove(car);
            await db.SaveChangesAsync();

            await LoadCarsAsync();
            ToastHelper.Show($"Автомобиль «{row.LicensePlate}» удалён", ToastType.Success);
        }
        catch (Exception ex)
        {
            ToastHelper.Show($"Ошибка удаления: {ex.Message}", ToastType.Error);
        }
    }

    private async Task ExecuteDeleteAsync()
    {
        if (SelectedCar is null) return;
        try
        {
            await using var db = new CarCareDbContext();

            bool hasOrders = await db.Orders.AnyAsync(o => o.CarID == SelectedCar.CarID);
            if (hasOrders)
            {
                ToastHelper.Show($"Нельзя удалить «{SelectedCar.LicensePlate}»: есть связанные заказы", ToastType.Warning);
                return;
            }

            var car = await db.Cars.FindAsync(SelectedCar.CarID);
            if (car is null) return;
            db.Cars.Remove(car);
            await db.SaveChangesAsync();

            var plate = SelectedCar.LicensePlate;
            await LoadCarsAsync();
            ToastHelper.Show($"Автомобиль «{plate}» удалён", ToastType.Success);
        }
        catch (Exception ex)
        {
            ToastHelper.Show($"Ошибка удаления: {ex.Message}", ToastType.Error);
        }
    }

    private void ExecuteViewOwner(CarRowDto? row)
    {
        if (row is null) return;
        if (Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
            mainVm.NavigateToClientsWithSearch(row.ClientFullName);
    }
}
