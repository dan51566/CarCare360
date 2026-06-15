using CarCare360.Desktop.Data;
using CarCare360.Desktop.Helpers;
using CarCare360.Desktop.Models;
using CarCare360.Desktop.Views;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CarCare360.Desktop.ViewModels;

// ═══════════════════════════════════════════════════════════════════════════════
// ClientMainViewModel — навигация клиентского окна
// ═══════════════════════════════════════════════════════════════════════════════

public sealed class ClientMainViewModel : BaseViewModel
{
    private object?      _currentView;
    private ImageSource? _sidebarAvatarSource;

    public ClientMainViewModel()
    {
        NavigateCarsCommand     = new RelayCommand(() => CurrentView = new ClientCarsView());
        NavigateOrdersCommand   = new RelayCommand(() => CurrentView = new ClientOrdersView());
        NavigateServicesCommand = new RelayCommand(() => CurrentView = new ServicesPublicView());
        NavigateProfileCommand  = new RelayCommand(() => CurrentView = new ProfileView());
        LogoutCommand           = new RelayCommand(ExecuteLogout);

        CurrentView = new ClientCarsView();
        LoadSidebarAvatar();
        UserAvatarStorage.AvatarChanged += OnAvatarChanged;
    }

    public string UserShortName
    {
        get
        {
            var parts = (CurrentUser.FullName ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "Клиент";
            if (parts.Length == 1) return parts[0];
            return $"{parts[1]} {parts[0][0]}.{(parts.Length > 2 ? " " + parts[2][0] + "." : "")}";
        }
    }

    public string UserInitials
    {
        get
        {
            var parts = (CurrentUser.FullName ?? "?")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant()
                : parts.Length == 1 ? parts[0][..1].ToUpperInvariant() : "?";
        }
    }

    public ImageSource? SidebarAvatarSource
    {
        get => _sidebarAvatarSource;
        private set { if (SetProperty(ref _sidebarAvatarSource, value)) OnPropertyChanged(nameof(HasSidebarAvatar)); }
    }

    public bool HasSidebarAvatar => _sidebarAvatarSource is not null;

    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public ICommand NavigateCarsCommand     { get; }
    public ICommand NavigateOrdersCommand   { get; }
    public ICommand NavigateServicesCommand { get; }
    public ICommand NavigateProfileCommand  { get; }
    public ICommand LogoutCommand           { get; }

    private void OnAvatarChanged(object? sender, int userId)
    { if (userId == CurrentUser.UserID) LoadSidebarAvatar(); }

    private void LoadSidebarAvatar()
    {
        var path = UserAvatarStorage.GetAvatarPath(CurrentUser.UserID);
        if (path is not null && File.Exists(path))
        {
            try
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.UriSource   = new Uri(path, UriKind.Absolute);
                img.EndInit();
                img.Freeze();
                SidebarAvatarSource = img;
                return;
            }
            catch { }
        }
        SidebarAvatarSource = null;
    }

    private static void ExecuteLogout()
    {
        CurrentUser.Logout();
        new LoginWindow().Show();
        foreach (Window w in Application.Current.Windows)
        {
            if (w is ClientWindow cw) { cw.Close(); break; }
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ClientCarsViewModel — автомобили текущего клиента (только просмотр)
// ═══════════════════════════════════════════════════════════════════════════════

public sealed record ClientCarRow(
    int     CarID,
    string  LicensePlate,
    string  BrandModel,
    int     Year,
    string? VIN,
    string? Color,
    int     Mileage
);

public sealed class ClientCarsViewModel : BaseViewModel
{
    private bool   _isLoading;
    private string _status = string.Empty;
    private ObservableCollection<ClientCarRow> _cars = [];

    public ClientCarsViewModel()
    {
        RefreshCommand = new RelayCommand(async () => await LoadAsync());
        AddCarCommand  = new RelayCommand(ExecuteAddCar);
        _ = LoadAsync();
    }

    public bool   IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public string Status    { get => _status;    set => SetProperty(ref _status, value); }

    public ObservableCollection<ClientCarRow> Cars
    {
        get => _cars;
        private set => SetProperty(ref _cars, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand AddCarCommand  { get; }

    private void ExecuteAddCar()
    {
        var dialog = new ClientCarAddDialog();
        DialogHelper.SetOwner(dialog);
        if (dialog.ShowDialog() == true)
            _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await using var db = new CarCareDbContext();
            var rows = await db.Cars
                .Where(c => c.ClientID == CurrentUser.UserID)
                .Include(c => c.Model).ThenInclude(m => m!.Brand)
                .OrderBy(c => c.LicensePlate)
                .Select(c => new ClientCarRow(
                    c.CarID,
                    c.LicensePlate,
                    (c.Model != null && c.Model.Brand != null ? c.Model.Brand.Name : "") + " " +
                    (c.Model != null ? c.Model.Name : ""),
                    c.Year ?? 0,
                    c.VIN,
                    c.Color,
                    c.Mileage ?? 0))
                .ToListAsync();

            Cars   = new ObservableCollection<ClientCarRow>(rows);
            Status = $"Автомобилей: {rows.Count}";
        }
        catch (Exception ex) { Status = $"Ошибка: {ex.Message}"; }
        finally { IsLoading = false; }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ClientOrdersViewModel — заказы текущего клиента (только просмотр)
// ═══════════════════════════════════════════════════════════════════════════════

public sealed record ClientOrderRow(
    int       OrderID,
    string    CarInfo,
    string    Status,
    DateTime? ScheduledDate,
    DateTime? CreatedAt,
    decimal   TotalCost
);

public sealed class ClientOrdersViewModel : BaseViewModel
{
    private bool   _isLoading;
    private string _status = string.Empty;
    private ObservableCollection<ClientOrderRow> _orders = [];

    public ClientOrdersViewModel()
    {
        RefreshCommand      = new RelayCommand(async () => await LoadAsync());
        CreateOrderCommand  = new RelayCommand(ExecuteCreateOrder);
        ViewOrderCommand    = new RelayCommand<ClientOrderRow?>(ExecuteViewOrder);
        _ = LoadAsync();
    }

    public bool   IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public string Status    { get => _status;    set => SetProperty(ref _status, value); }

    public ObservableCollection<ClientOrderRow> Orders
    {
        get => _orders;
        private set => SetProperty(ref _orders, value);
    }

    public ICommand RefreshCommand     { get; }
    public ICommand CreateOrderCommand { get; }
    public ICommand ViewOrderCommand   { get; }

    private void ExecuteCreateOrder()
    {
        var dialog = new ClientOrderCreateDialog();
        DialogHelper.SetOwner(dialog);
        if (dialog.ShowDialog() == true)
            _ = LoadAsync();
    }

    private void ExecuteViewOrder(ClientOrderRow? row)
    {
        if (row is null) return;
        var dialog = new ClientOrderDetailDialog(row.OrderID);
        DialogHelper.SetOwner(dialog);
        dialog.ShowDialog();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await using var db = new CarCareDbContext();
            var raw = await db.Orders
                .Where(o => o.ClientID == CurrentUser.UserID)
                .Include(o => o.Car).ThenInclude(c => c!.Model).ThenInclude(m => m!.Brand)
                .Include(o => o.OrderServices).ThenInclude(os => os.Service)
                .Include(o => o.OrderParts)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var rows = raw.Select(o => new ClientOrderRow(
                o.OrderID,
                o.Car != null
                    ? $"{o.Car.Model?.Brand?.Name} {o.Car.Model?.Name} · {o.Car.LicensePlate}".Trim()
                    : "—",
                o.Status ?? "—",
                o.ScheduledDate,
                o.CreatedAt,
                o.OrderServices.Sum(os => os.Service?.BasePrice ?? 0m) +
                o.OrderParts.Sum(op => (op.PricePerUnit ?? 0m) * op.Quantity)
            )).ToList();

            Orders = new ObservableCollection<ClientOrderRow>(rows);
            Status = $"Заказов: {rows.Count}";
        }
        catch (Exception ex) { Status = $"Ошибка: {ex.Message}"; }
        finally { IsLoading = false; }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ClientCarAddViewModel — диалог добавления автомобиля клиентом
// ═══════════════════════════════════════════════════════════════════════════════

public sealed class ClientCarAddViewModel : BaseViewModel
{
    private ObservableCollection<CarBrand> _brands  = [];
    private ObservableCollection<CarModel> _models  = [];
    private CarBrand? _selectedBrand;
    private CarModel? _selectedModel;
    private string  _licensePlate = string.Empty;
    private string  _vin          = string.Empty;
    private string  _color        = string.Empty;
    private string  _yearText     = string.Empty;
    private string  _mileageText  = string.Empty;
    private string  _errorMessage = string.Empty;
    private bool    _isBusy;

    public ClientCarAddViewModel()
    {
        SaveCommand   = new RelayCommand(async () => await ExecuteSaveAsync(), CanSave);
        CancelCommand = new RelayCommand(ExecuteCancel);
        _ = LoadBrandsAsync();
    }

    public ObservableCollection<CarBrand> Brands
    {
        get => _brands;
        private set => SetProperty(ref _brands, value);
    }

    public ObservableCollection<CarModel> Models
    {
        get => _models;
        private set => SetProperty(ref _models, value);
    }

    public CarBrand? SelectedBrand
    {
        get => _selectedBrand;
        set
        {
            if (SetProperty(ref _selectedBrand, value))
            {
                SelectedModel = null;
                _ = LoadModelsAsync(value?.BrandID);
            }
        }
    }

    public CarModel? SelectedModel
    {
        get => _selectedModel;
        set => SetProperty(ref _selectedModel, value);
    }

    public string LicensePlate
    {
        get => _licensePlate;
        set { SetProperty(ref _licensePlate, value); ((RelayCommand)SaveCommand).RaiseCanExecuteChanged(); }
    }

    public string VIN          { get => _vin;          set => SetProperty(ref _vin, value); }
    public string Color        { get => _color;        set => SetProperty(ref _color, value); }
    public string YearText     { get => _yearText;     set => SetProperty(ref _yearText, value); }
    public string MileageText  { get => _mileageText;  set => SetProperty(ref _mileageText, value); }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); }
    }

    public bool HasError => !string.IsNullOrEmpty(_errorMessage);
    public bool IsBusy   { get => _isBusy; private set => SetProperty(ref _isBusy, value); }

    public ICommand SaveCommand   { get; }
    public ICommand CancelCommand { get; }

    public bool? DialogResult { get; private set; }

    public event EventHandler? RequestClose;

    private bool CanSave() => !string.IsNullOrWhiteSpace(LicensePlate) && SelectedModel != null;

    private async Task ExecuteSaveAsync()
    {
        ErrorMessage = string.Empty;
        if (SelectedModel == null)     { ErrorMessage = "Выберите марку и модель.";        return; }
        if (string.IsNullOrWhiteSpace(LicensePlate)) { ErrorMessage = "Укажите гос. номер."; return; }

        int? year    = int.TryParse(YearText,    out var y) ? y : null;
        int? mileage = int.TryParse(MileageText, out var m) ? m : null;

        IsBusy = true;
        try
        {
            await using var db = new CarCareDbContext();
            var car = new Car
            {
                ClientID     = CurrentUser.UserID,
                ModelID      = SelectedModel.ModelID,
                LicensePlate = LicensePlate.Trim().ToUpperInvariant(),
                VIN          = string.IsNullOrWhiteSpace(VIN)    ? null : VIN.Trim().ToUpperInvariant(),
                Color        = string.IsNullOrWhiteSpace(Color)  ? null : Color.Trim(),
                Year         = year,
                Mileage      = mileage
            };
            db.Cars.Add(car);
            await db.SaveChangesAsync();

            DialogResult = true;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка сохранения: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    private void ExecuteCancel()
    {
        DialogResult = false;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private async Task LoadBrandsAsync()
    {
        await using var db = new CarCareDbContext();
        var list = await db.CarBrands.OrderBy(b => b.Name).ToListAsync();
        Brands = new ObservableCollection<CarBrand>(list);
    }

    private async Task LoadModelsAsync(int? brandId)
    {
        if (brandId is null) { Models = []; return; }
        await using var db = new CarCareDbContext();
        var list = await db.CarModels
            .Where(m => m.BrandID == brandId)
            .OrderBy(m => m.Name)
            .ToListAsync();
        Models = new ObservableCollection<CarModel>(list);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ClientOrderCreateViewModel — диалог создания заявки клиентом
// ═══════════════════════════════════════════════════════════════════════════════

public sealed record ClientCarSimple(int CarID, string DisplayName);

public sealed class ClientOrderCreateViewModel : BaseViewModel
{
    private ObservableCollection<ClientCarSimple> _cars = [];
    private ClientCarSimple? _selectedCar;
    private string   _notes        = string.Empty;
    private DateTime? _scheduledDate;
    private string   _errorMessage = string.Empty;
    private bool     _isBusy;

    public ClientOrderCreateViewModel()
    {
        SaveCommand   = new RelayCommand(async () => await ExecuteSaveAsync(), CanSave);
        CancelCommand = new RelayCommand(ExecuteCancel);
        _ = LoadCarsAsync();
    }

    public ObservableCollection<ClientCarSimple> Cars
    {
        get => _cars;
        private set => SetProperty(ref _cars, value);
    }

    public ClientCarSimple? SelectedCar
    {
        get => _selectedCar;
        set { SetProperty(ref _selectedCar, value); ((RelayCommand)SaveCommand).RaiseCanExecuteChanged(); }
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public DateTime? ScheduledDate
    {
        get => _scheduledDate;
        set => SetProperty(ref _scheduledDate, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); }
    }

    public bool HasError => !string.IsNullOrEmpty(_errorMessage);
    public bool IsBusy   { get => _isBusy; private set => SetProperty(ref _isBusy, value); }

    public ICommand SaveCommand   { get; }
    public ICommand CancelCommand { get; }

    public bool? DialogResult { get; private set; }
    public event EventHandler? RequestClose;

    private bool CanSave() => SelectedCar != null;

    private async Task ExecuteSaveAsync()
    {
        ErrorMessage = string.Empty;
        if (SelectedCar == null) { ErrorMessage = "Выберите автомобиль."; return; }

        IsBusy = true;
        try
        {
            await using var db = new CarCareDbContext();
            var order = new Order
            {
                CarID         = SelectedCar.CarID,
                ClientID      = CurrentUser.UserID,
                Status        = "Новый",
                Notes         = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                ScheduledDate = ScheduledDate,
                CreatedAt     = DateTime.Now,
                IsDeleted     = false
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            DialogResult = true;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка создания заказа: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    private void ExecuteCancel()
    {
        DialogResult = false;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private async Task LoadCarsAsync()
    {
        await using var db = new CarCareDbContext();
        var list = await db.Cars
            .Where(c => c.ClientID == CurrentUser.UserID)
            .Include(c => c.Model).ThenInclude(m => m!.Brand)
            .OrderBy(c => c.LicensePlate)
            .Select(c => new ClientCarSimple(
                c.CarID,
                (c.Model != null && c.Model.Brand != null ? c.Model.Brand.Name + " " : "") +
                (c.Model != null ? c.Model.Name + " " : "") +
                c.LicensePlate))
            .ToListAsync();
        Cars = new ObservableCollection<ClientCarSimple>(list);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ClientOrderDetailViewModel — детальный просмотр заказа клиентом + ответ
// ═══════════════════════════════════════════════════════════════════════════════

public sealed record ClientServiceLine(string ServiceName, decimal Price);
public sealed record ClientPartLine(string PartName, int Qty, decimal Price, decimal Total);

public sealed class ClientOrderDetailViewModel : BaseViewModel
{
    private readonly int _orderId;

    private string   _carInfo       = string.Empty;
    private string   _status        = string.Empty;
    private string   _createdAt     = string.Empty;
    private string   _scheduledDate = string.Empty;
    private string   _mechanicNotes = string.Empty;
    private string   _clientNotes   = string.Empty;
    private string   _errorMessage  = string.Empty;
    private bool     _isBusy;

    private ObservableCollection<ClientServiceLine> _services = [];
    private ObservableCollection<ClientPartLine>    _parts    = [];

    public ClientOrderDetailViewModel(int orderId)
    {
        _orderId = orderId;

        AgreeCommand   = new RelayCommand(async () => await ExecuteAgreeAsync(),   () => IsClientPending && !IsBusy);
        RejectCommand  = new RelayCommand(async () => await ExecuteRejectAsync(),  () => IsClientPending && !IsBusy && !string.IsNullOrWhiteSpace(ClientNotes));
        CancelCommand  = new RelayCommand(async () => await ExecuteCancelAsync(),  () => IsClientPending && !IsBusy);
        CloseCommand   = new RelayCommand(() => RequestClose?.Invoke(this, EventArgs.Empty));

        _ = LoadAsync();
    }

    public string CarInfo       { get => _carInfo;       private set => SetProperty(ref _carInfo,       value); }
    public string Status        { get => _status;        private set { SetProperty(ref _status, value); OnPropertyChanged(nameof(IsClientPending)); OnPropertyChanged(nameof(StatusColor)); } }
    public string CreatedAt     { get => _createdAt;     private set => SetProperty(ref _createdAt,     value); }
    public string ScheduledDate { get => _scheduledDate; private set => SetProperty(ref _scheduledDate, value); }
    public string MechanicNotes { get => _mechanicNotes; private set => SetProperty(ref _mechanicNotes, value); }
    public string ErrorMessage  { get => _errorMessage;  private set => SetProperty(ref _errorMessage,  value); }
    public bool   IsBusy        { get => _isBusy;        private set => SetProperty(ref _isBusy,        value); }

    public string ClientNotes
    {
        get => _clientNotes;
        set
        {
            SetProperty(ref _clientNotes, value);
            ((RelayCommand)RejectCommand).RaiseCanExecuteChanged();
        }
    }

    public ObservableCollection<ClientServiceLine> Services
    {
        get => _services;
        private set => SetProperty(ref _services, value);
    }

    public ObservableCollection<ClientPartLine> Parts
    {
        get => _parts;
        private set => SetProperty(ref _parts, value);
    }

    public decimal ServicesTotalCost => Services.Sum(s => s.Price);
    public decimal PartsTotalCost    => Parts.Sum(p => p.Total);
    public decimal GrandTotal        => ServicesTotalCost + PartsTotalCost;

    // Кнопки ответа показываются только когда заказ в статусе «Новый»
    public bool IsClientPending => Status == "Новый";

    public string StatusColor => Status switch
    {
        "Новый"            => "#2196F3",
        "Назначен"         => "#FF9800",
        "В работе"         => "#FFC107",
        "Ожидает запчасти" => "#9C27B0",
        "Готов"            => "#4CAF50",
        "Выдан"            => "#607D8B",
        "Отменён"          => "#F44336",
        _                  => "#888888"
    };

    public int OrderId => _orderId;

    public ICommand AgreeCommand  { get; }
    public ICommand RejectCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand CloseCommand  { get; }

    public bool? DialogResult { get; private set; }
    public event EventHandler? RequestClose;

    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            await using var db = new CarCareDbContext();
            var order = await db.Orders
                .Include(o => o.Car).ThenInclude(c => c!.Model).ThenInclude(m => m!.Brand)
                .Include(o => o.OrderServices).ThenInclude(os => os.Service)
                .Include(o => o.OrderParts).ThenInclude(op => op.Part)
                .FirstOrDefaultAsync(o => o.OrderID == _orderId && o.ClientID == CurrentUser.UserID);

            if (order is null) { ErrorMessage = "Заказ не найден."; return; }

            CarInfo       = $"{order.Car?.Model?.Brand?.Name} {order.Car?.Model?.Name} · {order.Car?.LicensePlate}".Trim();
            Status        = order.Status ?? "Новый";
            CreatedAt     = order.CreatedAt?.ToString("dd.MM.yyyy HH:mm") ?? "—";
            ScheduledDate = order.ScheduledDate?.ToString("dd.MM.yyyy") ?? "—";
            MechanicNotes = order.Notes ?? string.Empty;

            Services = new ObservableCollection<ClientServiceLine>(
                order.OrderServices.Select(os => new ClientServiceLine(
                    os.Service?.Name ?? "—",
                    os.Service?.BasePrice ?? 0m)));

            Parts = new ObservableCollection<ClientPartLine>(
                order.OrderParts.Select(op => new ClientPartLine(
                    op.Part?.Name ?? "—",
                    op.Quantity,
                    op.PricePerUnit ?? 0m,
                    (op.PricePerUnit ?? 0m) * op.Quantity)));

            OnPropertyChanged(nameof(ServicesTotalCost));
            OnPropertyChanged(nameof(PartsTotalCost));
            OnPropertyChanged(nameof(GrandTotal));
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }

    private async Task ExecuteAgreeAsync()
    {
        await SetStatusAsync("Назначен");
    }

    private async Task ExecuteRejectAsync()
    {
        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            await using var db = new CarCareDbContext();
            var order = await db.Orders.FindAsync(_orderId);
            if (order is null) return;
            // Добавляем комментарий клиента к примечаниям
            var existing  = string.IsNullOrWhiteSpace(order.Notes) ? string.Empty : order.Notes + "\n\n";
            order.Notes   = existing + $"[Клиент {DateTime.Now:dd.MM HH:mm}]: {ClientNotes.Trim()}";
            await db.SaveChangesAsync();

            Status        = order.Status ?? Status;
            MechanicNotes = order.Notes;
            ClientNotes   = string.Empty;

            ErrorMessage = "Пожелание отправлено. Ожидайте ответа от мастера.";
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }

    private async Task ExecuteCancelAsync()
    {
        await SetStatusAsync("Отменён");
    }

    private async Task SetStatusAsync(string newStatus)
    {
        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            await using var db = new CarCareDbContext();
            var order = await db.Orders.FindAsync(_orderId);
            if (order is null) return;
            order.Status = newStatus;
            await db.SaveChangesAsync();
            Status = newStatus;
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ServicesPublicViewModel — прайс-лист услуг (только просмотр)
// ═══════════════════════════════════════════════════════════════════════════════

public sealed record ServicePublicRow(
    int     ServiceID,
    string  Name,
    decimal Price,
    decimal NormHour
);

public sealed class ServicesPublicViewModel : BaseViewModel
{
    private bool   _isLoading;
    private string _status = string.Empty;
    private ObservableCollection<ServicePublicRow> _services = [];

    public ServicesPublicViewModel()
    {
        RefreshCommand = new RelayCommand(async () => await LoadAsync());
        _ = LoadAsync();
    }

    public bool   IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public string Status    { get => _status;    set => SetProperty(ref _status, value); }

    public ObservableCollection<ServicePublicRow> Services
    {
        get => _services;
        private set => SetProperty(ref _services, value);
    }

    public ICommand RefreshCommand { get; }

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await using var db = new CarCareDbContext();
            var rows = await db.Services
                .OrderBy(s => s.Name)
                .Select(s => new ServicePublicRow(s.ServiceID, s.Name, s.BasePrice ?? 0m, s.NormHour))
                .ToListAsync();

            Services = new ObservableCollection<ServicePublicRow>(rows);
            Status   = $"Услуг: {rows.Count}";
        }
        catch (Exception ex) { Status = $"Ошибка: {ex.Message}"; }
        finally { IsLoading = false; }
    }
}
