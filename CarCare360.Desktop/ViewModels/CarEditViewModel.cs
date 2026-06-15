using CarCare360.Desktop.Data;
using CarCare360.Desktop.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CarCare360.Desktop.Helpers;

namespace CarCare360.Desktop.ViewModels;

/// <summary>Элемент списка клиентов в ComboBox диалога.</summary>
public record ClientItem(int UserID, string FullName, string? Phone)
{
    public override string ToString() =>
        string.IsNullOrEmpty(Phone) ? FullName : $"{FullName} ({Phone})";
}

/// <summary>
/// ViewModel диалога добавления / редактирования автомобиля.
///
/// Особенности:
///  — Каскадная загрузка: при выборе марки автоматически обновляется
///    список моделей (только для выбранной марки).
///  — При создании можно предустановить ClientID (например, при вызове
///    из карточки конкретного клиента).
/// </summary>
public sealed class CarEditViewModel : BaseViewModel
{
    private readonly int?      _carId;   // null — новый автомобиль
    private readonly Func<Task> _onSaved;

    private ClientItem?  _selectedClient;
    private CarBrand?    _selectedBrand;
    private CarModel?    _selectedModel;
    private string       _licensePlate = string.Empty;
    private string       _vin          = string.Empty;
    private string       _color        = string.Empty;
    private string       _yearText     = string.Empty;
    private string       _mileageText  = string.Empty;
    private string       _errorMessage = string.Empty;
    private bool         _isBusy;

    // ── Конструктор: новый автомобиль ─────────────────────────────────────

    public CarEditViewModel(Func<Task> onSaved, int? presetClientId = null)
    {
        _carId    = null;
        _onSaved  = onSaved;
        IsNewCar  = true;
        _presetClientId = presetClientId;

        SaveCommand   = new RelayCommand(async () => await SaveAsync(), CanSave);
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));

        _ = LoadDataAsync();
    }

    // ── Конструктор: редактирование ───────────────────────────────────────

    public CarEditViewModel(CarRowDto car, Func<Task> onSaved)
    {
        _carId   = car.CarID;
        _onSaved = onSaved;
        IsNewCar = false;
        _editCarSnapshot = car;

        _licensePlate = car.LicensePlate;
        _vin          = car.VIN   ?? string.Empty;
        _color        = car.Color ?? string.Empty;
        _yearText     = car.Year?.ToString()    ?? string.Empty;
        _mileageText  = car.Mileage?.ToString() ?? string.Empty;

        SaveCommand   = new RelayCommand(async () => await SaveAsync(), CanSave);
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));

        _ = LoadDataAsync(car.ClientID, car.BrandID, car.ModelID);
    }

    private readonly int?       _presetClientId;
    private readonly CarRowDto? _editCarSnapshot;

    // ── Справочники ───────────────────────────────────────────────────────

    public ObservableCollection<ClientItem> Clients { get; } = new();
    public ObservableCollection<CarBrand>   Brands  { get; } = new();
    public ObservableCollection<CarModel>   Models  { get; } = new();

    // ── Свойства ─────────────────────────────────────────────────────────

    public bool   IsNewCar { get; }
    public string Title    => IsNewCar ? "Новый автомобиль" : "Редактировать автомобиль";

    public ClientItem? SelectedClient
    {
        get => _selectedClient;
        set { SetProperty(ref _selectedClient, value); RaiseCanExecute(); }
    }

    public CarBrand? SelectedBrand
    {
        get => _selectedBrand;
        set
        {
            if (SetProperty(ref _selectedBrand, value))
            {
                SelectedModel = null;
                _ = LoadModelsForBrandAsync(value?.BrandID);
                RaiseCanExecute();
            }
        }
    }

    public CarModel? SelectedModel
    {
        get => _selectedModel;
        set { SetProperty(ref _selectedModel, value); RaiseCanExecute(); }
    }

    public string LicensePlate
    {
        get => _licensePlate;
        set { SetProperty(ref _licensePlate, value); RaiseCanExecute(); }
    }

    public string VIN
    {
        get => _vin;
        set => SetProperty(ref _vin, value);
    }

    public string Color
    {
        get => _color;
        set => SetProperty(ref _color, value);
    }

    /// <summary>Год выпуска — в виде строки для TextBox-привязки.</summary>
    public string YearText
    {
        get => _yearText;
        set => SetProperty(ref _yearText, value);
    }

    /// <summary>Пробег — в виде строки для TextBox-привязки.</summary>
    public string MileageText
    {
        get => _mileageText;
        set => SetProperty(ref _mileageText, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set { SetProperty(ref _isBusy, value); RaiseCanExecute(); }
    }

    // ── Команды ──────────────────────────────────────────────────────────

    public ICommand SaveCommand   { get; }
    public ICommand CancelCommand { get; }

    public event EventHandler? CloseRequested;

    // ── Загрузка справочников ─────────────────────────────────────────────

    private async Task LoadDataAsync(int? clientId = null, int? brandId = null, int? modelId = null)
    {
        IsBusy = true;
        try
        {
            await using var db = new CarCareDbContext();

            // Клиенты
            var clients = await db.Users
                .Include(u => u.Role)
                .Where(u => u.Role!.Name == "Клиент" && u.IsDeleted != true && u.IsActive == true)
                .OrderBy(u => u.FullName)
                .Select(u => new ClientItem(u.UserID, u.FullName, u.Phone))
                .ToListAsync();

            Clients.Clear();
            foreach (var c in clients) Clients.Add(c);

            // Если есть предустановленный клиент — выбираем его
            var targetClientId = clientId ?? _presetClientId;
            if (targetClientId.HasValue)
                SelectedClient = Clients.FirstOrDefault(c => c.UserID == targetClientId.Value);

            // Марки
            var brands = await db.CarBrands.OrderBy(b => b.Name).ToListAsync();
            Brands.Clear();
            foreach (var b in brands) Brands.Add(b);

            // Предустановленная марка
            if (brandId.HasValue)
            {
                SelectedBrand = Brands.FirstOrDefault(b => b.BrandID == brandId.Value);
                // Модели для этой марки загрузятся через setter SelectedBrand,
                // но нам нужно дождаться их и затем выбрать конкретную модель
                if (SelectedBrand is not null)
                {
                    await LoadModelsForBrandAsync(brandId, preSelectModelId: modelId);
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки данных: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadModelsForBrandAsync(int? brandId, int? preSelectModelId = null)
    {
        Models.Clear();
        if (brandId is null) return;

        try
        {
            await using var db = new CarCareDbContext();
            var models = await db.CarModels
                .Where(m => m.BrandID == brandId.Value)
                .OrderBy(m => m.Name)
                .ToListAsync();
            foreach (var m in models) Models.Add(m);

            if (preSelectModelId.HasValue)
                SelectedModel = Models.FirstOrDefault(m => m.ModelID == preSelectModelId.Value);
        }
        catch { /* игнорируем ошибки загрузки моделей */ }
    }

    // ── Логика ───────────────────────────────────────────────────────────

    private bool CanSave() =>
        !IsBusy &&
        SelectedClient is not null &&
        SelectedModel  is not null &&
        !string.IsNullOrWhiteSpace(LicensePlate);

    private void RaiseCanExecute() => CommandManager.InvalidateRequerySuggested();

    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        // Валидация
        if (SelectedClient is null) { ErrorMessage = "Выберите владельца.";        return; }
        if (SelectedBrand  is null) { ErrorMessage = "Выберите марку.";            return; }
        if (SelectedModel  is null) { ErrorMessage = "Выберите модель.";           return; }
        if (string.IsNullOrWhiteSpace(LicensePlate)) { ErrorMessage = "Введите гос. номер."; return; }

        // Парсинг числовых полей
        int? year    = null;
        int? mileage = null;
        if (!string.IsNullOrWhiteSpace(YearText))
        {
            if (!int.TryParse(YearText.Trim(), out var y) || y < 1900 || y > DateTime.Now.Year + 1)
            { ErrorMessage = "Некорректный год выпуска."; return; }
            year = y;
        }
        if (!string.IsNullOrWhiteSpace(MileageText))
        {
            if (!int.TryParse(MileageText.Trim(), out var m) || m < 0)
            { ErrorMessage = "Некорректный пробег."; return; }
            mileage = m;
        }

        IsBusy = true;
        try
        {
            await using var db = new CarCareDbContext();

            var plate = LicensePlate.Trim().ToUpperInvariant();

            if (IsNewCar)
            {
                // Проверка уникальности гос. номера
                bool plateExists = await db.Cars.AnyAsync(c => c.LicensePlate == plate);
                if (plateExists) { ErrorMessage = "Автомобиль с таким гос. номером уже существует."; return; }

                db.Cars.Add(new Car
                {
                    ClientID     = SelectedClient.UserID,
                    ModelID      = SelectedModel.ModelID,
                    LicensePlate = plate,
                    VIN          = string.IsNullOrWhiteSpace(VIN)   ? null : VIN.Trim().ToUpperInvariant(),
                    Color        = string.IsNullOrWhiteSpace(Color)  ? null : Color.Trim(),
                    Year         = year,
                    Mileage      = mileage
                });
            }
            else
            {
                var car = await db.Cars.FindAsync(_carId!.Value);
                if (car is null) { ErrorMessage = "Автомобиль не найден в базе данных."; return; }

                // Проверка уникальности если номер изменился
                if (!string.Equals(car.LicensePlate, plate, StringComparison.OrdinalIgnoreCase))
                {
                    bool plateExists = await db.Cars.AnyAsync(c =>
                        c.LicensePlate == plate && c.CarID != _carId.Value);
                    if (plateExists) { ErrorMessage = "Автомобиль с таким гос. номером уже существует."; return; }
                }

                car.ClientID     = SelectedClient.UserID;
                car.ModelID      = SelectedModel.ModelID;
                car.LicensePlate = plate;
                car.VIN          = string.IsNullOrWhiteSpace(VIN)  ? null : VIN.Trim().ToUpperInvariant();
                car.Color        = string.IsNullOrWhiteSpace(Color) ? null : Color.Trim();
                car.Year         = year;
                car.Mileage      = mileage;
            }

            await db.SaveChangesAsync();
            await _onSaved();
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.InnerException?.Message ?? ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
