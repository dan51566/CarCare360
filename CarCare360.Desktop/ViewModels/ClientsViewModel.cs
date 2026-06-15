using CarCare360.Desktop.Data;
using CarCare360.Desktop.Helpers;
using CarCare360.Desktop.Views;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace CarCare360.Desktop.ViewModels;

/// <summary>
/// DTO для отображения одной строки клиента в DataGrid.
/// </summary>
public record ClientRowDto(
    int       UserID,
    string    FullName,
    string?   Phone,
    string?   Email,
    bool      IsActive,
    bool      IsDeleted,
    DateTime? CreatedAt,
    int       CarCount,
    string    Login
);

/// <summary>
/// ViewModel раздела «Клиенты».
/// </summary>
public sealed class ClientsViewModel : BaseViewModel
{
    private string _searchText = string.Empty;
    private ObservableCollection<ClientRowDto> _clients = new();
    private ClientRowDto? _selectedClient;
    private bool _showDeleted;
    private bool _isLoading;
    private string _statusMessage = string.Empty;

    public ClientsViewModel()
    {
        RefreshCommand = new RelayCommand(async () => await LoadClientsAsync());
        AddCommand     = new RelayCommand(ExecuteAdd);

        // Команды тулбара (устаревшие — оставлены для совместимости, но скрыты в новом UI)
        EditCommand    = new RelayCommand(ExecuteEdit,
                            () => SelectedClient is { IsDeleted: false });
        DeleteCommand  = new RelayCommand(async () => await ExecuteDeleteAsync(),
                            () => SelectedClient is { IsDeleted: false });
        RestoreCommand = new RelayCommand(async () => await ExecuteRestoreAsync(),
                            () => SelectedClient is { IsDeleted: true });

        // Строчные команды — принимают ClientRowDto через CommandParameter
        InlineEditCommand    = new RelayCommand<ClientRowDto>(ExecuteInlineEdit,
                                   row => row is { IsDeleted: false });
        InlineDeleteCommand  = new RelayCommand<ClientRowDto>(
                                   async row => await ExecuteInlineDeleteAsync(row),
                                   row => row is { IsDeleted: false });
        InlineRestoreCommand = new RelayCommand<ClientRowDto>(
                                   async row => await ExecuteInlineRestoreAsync(row),
                                   row => row is { IsDeleted: true });

        // Переход в «Автомобили» с фильтром по клиенту
        ViewClientCarsCommand = new RelayCommand<ClientRowDto>(ExecuteViewClientCars,
                                    row => row is { CarCount: > 0 });
    }

    // ── Свойства ──────────────────────────────────────────────────────────

    public string SearchText
    {
        get => _searchText;
        set { if (SetProperty(ref _searchText, value)) _ = LoadClientsAsync(); }
    }

    public ObservableCollection<ClientRowDto> Clients
    {
        get => _clients;
        private set => SetProperty(ref _clients, value);
    }

    public ClientRowDto? SelectedClient
    {
        get => _selectedClient;
        set
        {
            SetProperty(ref _selectedClient, value);
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool ShowDeleted
    {
        get => _showDeleted;
        set { if (SetProperty(ref _showDeleted, value)) _ = LoadClientsAsync(); }
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

    public ICommand RefreshCommand      { get; }
    public ICommand AddCommand          { get; }
    public ICommand EditCommand         { get; }
    public ICommand DeleteCommand       { get; }
    public ICommand RestoreCommand      { get; }

    /// <summary>Редактировать клиента прямо из строки таблицы (CommandParameter = ClientRowDto).</summary>
    public ICommand InlineEditCommand    { get; }
    /// <summary>Удалить клиента прямо из строки таблицы.</summary>
    public ICommand InlineDeleteCommand  { get; }
    /// <summary>Восстановить клиента прямо из строки таблицы.</summary>
    public ICommand InlineRestoreCommand { get; }
    /// <summary>Перейти в «Автомобили» с фильтром по данному клиенту.</summary>
    public ICommand ViewClientCarsCommand { get; }

    // ── Загрузка ─────────────────────────────────────────────────────────

    public async Task LoadClientsAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            await using var db = new CarCareDbContext();
            var search = SearchText.Trim();

            var query = db.Users
                .Include(u => u.Role)
                .Where(u => u.Role!.Name == "Клиент");

            if (!ShowDeleted)
                query = query.Where(u => u.IsDeleted != true);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(u =>
                    u.FullName.Contains(search) ||
                    (u.Phone != null && u.Phone.Contains(search)));

            var rows = await query
                .OrderBy(u => u.FullName)
                .Select(u => new
                {
                    u.UserID, u.FullName, u.Phone, u.Email,
                    u.IsActive, u.IsDeleted, u.CreatedAt, u.Login,
                    CarCount = db.Cars.Count(c => c.ClientID == u.UserID)
                })
                .ToListAsync();

            Clients = new ObservableCollection<ClientRowDto>(
                rows.Select(r => new ClientRowDto(
                    r.UserID, r.FullName, r.Phone, r.Email,
                    r.IsActive ?? true, r.IsDeleted ?? false,
                    r.CreatedAt, r.CarCount, r.Login)));

            StatusMessage = $"Клиентов: {rows.Count}";
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
        var vm = new ClientEditViewModel(onSaved: async () => await LoadClientsAsync());
        var dialog = new ClientEditDialog { DataContext = vm };
        vm.CloseRequested += (_, _) => dialog.Close();
        DialogHelper.SetOwner(dialog);
        dialog.ShowDialog();
        ToastHelper.Show("Клиент успешно добавлен.");
    }

    private void ExecuteEdit()
    {
        if (SelectedClient is null) return;
        ExecuteInlineEdit(SelectedClient);
    }

    private void ExecuteInlineEdit(ClientRowDto? row)
    {
        if (row is null or { IsDeleted: true }) return;
        var vm = new ClientEditViewModel(row, onSaved: async () => await LoadClientsAsync());
        var dialog = new ClientEditDialog { DataContext = vm };
        vm.CloseRequested += (_, _) => dialog.Close();
        DialogHelper.SetOwner(dialog);
        dialog.ShowDialog();
    }

    private async Task ExecuteDeleteAsync()
    {
        if (SelectedClient is null) return;
        await ExecuteInlineDeleteAsync(SelectedClient);
    }

    private async Task ExecuteInlineDeleteAsync(ClientRowDto? row)
    {
        if (row is null or { IsDeleted: true }) return;
        try
        {
            await using var db = new CarCareDbContext();
            var user = await db.Users.FindAsync(row.UserID);
            if (user is null) return;
            user.IsDeleted = true;
            user.IsActive  = false;
            await db.SaveChangesAsync();
            await LoadClientsAsync();
            ToastHelper.Show($"Клиент «{row.FullName}» удалён.", ToastType.Warning);
        }
        catch (Exception ex)
        {
            ToastHelper.Show($"Ошибка: {ex.Message}", ToastType.Error);
        }
    }

    private async Task ExecuteRestoreAsync()
    {
        if (SelectedClient is null) return;
        await ExecuteInlineRestoreAsync(SelectedClient);
    }

    private async Task ExecuteInlineRestoreAsync(ClientRowDto? row)
    {
        if (row is null or { IsDeleted: false }) return;
        try
        {
            await using var db = new CarCareDbContext();
            var user = await db.Users.FindAsync(row.UserID);
            if (user is null) return;
            user.IsDeleted = false;
            user.IsActive  = true;
            await db.SaveChangesAsync();
            await LoadClientsAsync();
            ToastHelper.Show($"Клиент «{row.FullName}» восстановлен.", ToastType.Info);
        }
        catch (Exception ex)
        {
            ToastHelper.Show($"Ошибка: {ex.Message}", ToastType.Error);
        }
    }

    /// <summary>
    /// Навигация в «Автомобили» с фильтром по выбранному клиенту.
    /// Обращается к MainViewModel через Application.Current.MainWindow.DataContext.
    /// </summary>
    private void ExecuteViewClientCars(ClientRowDto? row)
    {
        if (row is null) return;
        if (Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
            mainVm.NavigateToCarsForClient(row.UserID);
    }
}
