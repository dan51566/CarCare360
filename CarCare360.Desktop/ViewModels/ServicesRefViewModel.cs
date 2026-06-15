using CarCare360.Desktop.Data;
using CarCare360.Desktop.Helpers;
using CarCare360.Desktop.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CarCare360.Desktop.ViewModels;

public record ServiceRefDto(int ServiceID, string Name, string? Description, decimal NormHour, decimal BasePrice);

/// <summary>ViewModel вкладки «Услуги» в разделе Справочники.</summary>
public sealed class ServicesRefViewModel : BaseViewModel
{
    private ServiceRefDto? _selectedItem;
    private bool  _isEditing;
    private string _searchText      = string.Empty;
    private string _editName        = string.Empty;
    private string _editDescription = string.Empty;
    private string _editNormHour    = "1";
    private string _editPrice       = "0";
    private string _statusMessage   = string.Empty;
    private int?   _editingId;      // null — новая

    public ServicesRefViewModel()
    {
        RefreshCommand    = new RelayCommand(async () => await LoadAsync());
        ShowAddCommand    = new RelayCommand(StartAdd);
        ShowEditCommand   = new RelayCommand(StartEdit, () => SelectedItem is not null);
        SaveEditCommand   = new RelayCommand(async () => await SaveAsync(), CanSave);
        CancelEditCommand = new RelayCommand(CancelEdit);
        DeleteCommand     = new RelayCommand(async () => await DeleteAsync(),
                               () => SelectedItem is not null);
        _ = LoadAsync();
    }

    public ObservableCollection<ServiceRefDto> Items { get; } = new();

    public string SearchText
    {
        get => _searchText;
        set { if (SetProperty(ref _searchText, value)) _ = LoadAsync(); }
    }

    public ServiceRefDto? SelectedItem
    {
        get => _selectedItem;
        set { SetProperty(ref _selectedItem, value); CommandManager.InvalidateRequerySuggested(); }
    }

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    public string EditName
    {
        get => _editName;
        set { SetProperty(ref _editName, value); RaiseCanExecute(); }
    }

    public string EditDescription
    {
        get => _editDescription;
        set => SetProperty(ref _editDescription, value);
    }

    public string EditNormHour
    {
        get => _editNormHour;
        set => SetProperty(ref _editNormHour, value);
    }

    public string EditPrice
    {
        get => _editPrice;
        set => SetProperty(ref _editPrice, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand RefreshCommand    { get; }
    public ICommand ShowAddCommand    { get; }
    public ICommand ShowEditCommand   { get; }
    public ICommand SaveEditCommand   { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand DeleteCommand     { get; }

    public async Task LoadAsync()
    {
        try
        {
            await using var db = new CarCareDbContext();
            var search = SearchText.Trim();
            var list = await db.Services
                .Where(s => string.IsNullOrEmpty(search) || s.Name.Contains(search))
                .OrderBy(s => s.Name)
                .ToListAsync();
            Items.Clear();
            foreach (var s in list)
                Items.Add(new ServiceRefDto(s.ServiceID, s.Name, s.Description, s.NormHour, s.BasePrice ?? 0m));
            StatusMessage = $"Услуг: {Items.Count}";
        }
        catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
    }

    private void StartAdd()
    {
        _editingId      = null;
        EditName        = string.Empty;
        EditDescription = string.Empty;
        EditNormHour    = "1";
        EditPrice       = "0";
        IsEditing       = true;
    }

    private void StartEdit()
    {
        if (SelectedItem is null) return;
        _editingId      = SelectedItem.ServiceID;
        EditName        = SelectedItem.Name;
        EditDescription = SelectedItem.Description ?? string.Empty;
        EditNormHour    = SelectedItem.NormHour.ToString("F1");
        EditPrice       = SelectedItem.BasePrice.ToString("F2");
        IsEditing       = true;
    }

    private void CancelEdit() { IsEditing = false; }

    private bool CanSave() => !string.IsNullOrWhiteSpace(EditName);
    private void RaiseCanExecute() => CommandManager.InvalidateRequerySuggested();

    private async Task SaveAsync()
    {
        if (!decimal.TryParse(EditNormHour.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal normHour) || normHour < 0)
        { StatusMessage = "Норм.часы — число ≥ 0."; return; }

        if (!decimal.TryParse(EditPrice.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal price) || price < 0)
        { StatusMessage = "Цена — число ≥ 0."; return; }

        try
        {
            await using var db = new CarCareDbContext();
            if (_editingId is null)
            {
                db.Services.Add(new Service
                {
                    Name        = EditName.Trim(),
                    Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription.Trim(),
                    NormHour    = normHour,
                    BasePrice   = price
                });
            }
            else
            {
                var e = await db.Services.FindAsync(_editingId.Value);
                if (e is not null)
                {
                    e.Name        = EditName.Trim();
                    e.Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription.Trim();
                    e.NormHour    = normHour;
                    e.BasePrice   = price;
                }
            }
            await db.SaveChangesAsync();
            IsEditing = false;
            await LoadAsync();
        }
        catch (Exception ex) { StatusMessage = $"Ошибка: {ex.InnerException?.Message ?? ex.Message}"; }
    }

    private async Task DeleteAsync()
    {
        if (SelectedItem is null) return;
        try
        {
            await using var db = new CarCareDbContext();
            bool inUse = await db.OrderServices.AnyAsync(os => os.ServiceID == SelectedItem.ServiceID);
            if (inUse) { StatusMessage = $"Нельзя удалить «{SelectedItem.Name}» — используется в заказах."; return; }

            var e = await db.Services.FindAsync(SelectedItem.ServiceID);
            if (e is not null) { db.Services.Remove(e); await db.SaveChangesAsync(); }
            await LoadAsync();
        }
        catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
    }
}
