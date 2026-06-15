using CarCare360.Desktop.Data;
using CarCare360.Desktop.Helpers;
using CarCare360.Desktop.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows.Input;

namespace CarCare360.Desktop.ViewModels;

/// <summary>
/// ViewModel диалога создания нового заказ-наряда.
/// Клиент → каскадная загрузка автомобилей → вызов CreateOrder SP.
/// </summary>
public sealed class OrderCreateViewModel : BaseViewModel
{
    private readonly Func<Task> _onSaved;

    private ClientItem?  _selectedClient;
    private CarRowDto?   _selectedCar;
    private string       _scheduledDate = string.Empty;
    private string       _scheduledTime = string.Empty;
    private string       _notes         = string.Empty;
    private string       _errorMessage  = string.Empty;
    private bool         _isBusy;

    public OrderCreateViewModel(Func<Task> onSaved)
    {
        _onSaved = onSaved;
        SaveCommand   = new RelayCommand(async () => await SaveAsync(), CanSave);
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));
        _ = LoadClientsAsync();
    }

    // ── Справочники ───────────────────────────────────────────────────────

    public ObservableCollection<ClientItem>  Clients    { get; } = new();
    public ObservableCollection<CarRowDto>   ClientCars { get; } = new();

    // ── Свойства ─────────────────────────────────────────────────────────

    public ClientItem? SelectedClient
    {
        get => _selectedClient;
        set
        {
            if (SetProperty(ref _selectedClient, value))
            {
                SelectedCar = null;
                _ = LoadClientCarsAsync(value?.UserID);
                RaiseCanExecute();
            }
        }
    }

    public CarRowDto? SelectedCar
    {
        get => _selectedCar;
        set { SetProperty(ref _selectedCar, value); RaiseCanExecute(); }
    }

    /// <summary>Дата — строка "dd.MM.yyyy" или "yyyy-MM-dd".</summary>
    public string ScheduledDate
    {
        get => _scheduledDate;
        set => SetProperty(ref _scheduledDate, value);
    }

    /// <summary>Время — строка "HH:mm".</summary>
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

    // ── Загрузка ─────────────────────────────────────────────────────────

    private async Task LoadClientsAsync()
    {
        IsBusy = true;
        try
        {
            await using var db = new CarCareDbContext();
            var clients = await db.Users
                .Include(u => u.Role)
                .Where(u => u.Role!.Name == "Клиент" && u.IsDeleted != true && u.IsActive == true)
                .OrderBy(u => u.FullName)
                .Select(u => new ClientItem(u.UserID, u.FullName, u.Phone))
                .ToListAsync();
            Clients.Clear();
            foreach (var c in clients) Clients.Add(c);
        }
        catch (Exception ex) { ErrorMessage = $"Ошибка загрузки клиентов: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    private async Task LoadClientCarsAsync(int? clientId)
    {
        ClientCars.Clear();
        if (clientId is null) return;
        try
        {
            await using var db = new CarCareDbContext();
            var cars = await db.Cars
                .Include(c => c.Model).ThenInclude(m => m!.Brand)
                .Where(c => c.ClientID == clientId.Value)
                .OrderBy(c => c.LicensePlate)
                .Select(c => new
                {
                    c.CarID, c.ClientID, c.ModelID,
                    BrandID       = c.Model!.BrandID,
                    BrandName     = c.Model!.Brand!.Name,
                    ModelName     = c.Model!.Name,
                    c.Year, c.LicensePlate, c.VIN, c.Color, c.Mileage,
                    ClientName    = c.Client!.FullName
                })
                .ToListAsync();
            foreach (var c in cars)
                ClientCars.Add(new CarRowDto(c.CarID, c.ClientID, c.ModelID, c.BrandID,
                    c.BrandName, c.ModelName, c.Year, c.LicensePlate, c.VIN, c.Color, c.Mileage, c.ClientName));
        }
        catch { /* тихо */ }
    }

    // ── Логика ───────────────────────────────────────────────────────────

    private bool CanSave() => !IsBusy && SelectedClient is not null && SelectedCar is not null;

    private void RaiseCanExecute() => CommandManager.InvalidateRequerySuggested();

    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;
        if (SelectedClient is null) { ErrorMessage = "Выберите клиента."; return; }
        if (SelectedCar    is null) { ErrorMessage = "Выберите автомобиль."; return; }

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
            var cs = db.Database.GetConnectionString()!;
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("CreateOrder", conn)
                { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@CarID",    SelectedCar.CarID);
            cmd.Parameters.AddWithValue("@ClientID", SelectedClient.UserID);
            cmd.Parameters.AddWithValue("@ScheduledDate",
                scheduledDate.HasValue ? (object)scheduledDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@ScheduledTime",
                scheduledTime.HasValue ? (object)scheduledTime.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Notes",
                string.IsNullOrWhiteSpace(Notes) ? DBNull.Value : (object)Notes.Trim());

            await cmd.ExecuteNonQueryAsync();

            await _onSaved();
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) { ErrorMessage = ex.InnerException?.Message ?? ex.Message; }
        finally { IsBusy = false; }
    }
}
