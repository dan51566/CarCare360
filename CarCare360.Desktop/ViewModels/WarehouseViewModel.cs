using CarCare360.Desktop.Data;
using CarCare360.Desktop.Helpers;
using CarCare360.Desktop.Views;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CarCare360.Desktop.ViewModels;

/// <summary>DTO строки запчасти для DataGrid склада.</summary>
public record PartRowDto(
    int      PartID,
    string   Name,
    string?  PartNumber,
    int      Stock,
    decimal  Price,
    decimal  TotalValue   // Price * Stock
)
{
    /// <summary>True когда остаток ≤ 5 — используется для красного текста в строке.</summary>
    public bool IsLowStock => Stock <= 5;
}

/// <summary>
/// ViewModel раздела «Склад».
/// Поиск по названию / артикулу, фильтр низкого остатка, CRUD + inline-команды.
/// </summary>
public sealed class WarehouseViewModel : BaseViewModel
{
    private string _searchText          = string.Empty;
    private bool   _showLowStock;
    private bool   _isLoading;
    private string _statusMessage       = string.Empty;
    private ObservableCollection<PartRowDto> _parts = new();
    private PartRowDto? _selectedPart;
    private int     _totalPartsCount;
    private int     _lowStockCount;
    private decimal _warehouseTotalValue;

    /// <summary>True для роли «Механик» — скрывает CRUD-кнопки в XAML.</summary>
    public bool IsReadOnly => string.Equals(CurrentUser.RoleName, "Механик", StringComparison.OrdinalIgnoreCase);

    public WarehouseViewModel()
    {
        RefreshCommand      = new RelayCommand(async () => await LoadPartsAsync());
        AddCommand          = new RelayCommand(ExecuteAdd);
        EditCommand         = new RelayCommand(ExecuteEdit, () => SelectedPart is not null);
        DeleteCommand       = new RelayCommand(async () => await ExecuteDeleteAsync(),
                                  () => SelectedPart is not null);
        InlineEditCommand   = new RelayCommand<PartRowDto>(ExecuteInlineEdit);
        InlineDeleteCommand = new RelayCommand<PartRowDto>(async r => await ExecuteInlineDeleteAsync(r));
    }

    // ── Свойства ─────────────────────────────────────────────────────────

    public string SearchText
    {
        get => _searchText;
        set { if (SetProperty(ref _searchText, value)) _ = LoadPartsAsync(); }
    }

    public bool ShowLowStock
    {
        get => _showLowStock;
        set { if (SetProperty(ref _showLowStock, value)) _ = LoadPartsAsync(); }
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

    public ObservableCollection<PartRowDto> Parts
    {
        get => _parts;
        private set => SetProperty(ref _parts, value);
    }

    public PartRowDto? SelectedPart
    {
        get => _selectedPart;
        set { SetProperty(ref _selectedPart, value); CommandManager.InvalidateRequerySuggested(); }
    }

    public int TotalPartsCount
    {
        get => _totalPartsCount;
        private set => SetProperty(ref _totalPartsCount, value);
    }

    public int LowStockCount
    {
        get => _lowStockCount;
        private set => SetProperty(ref _lowStockCount, value);
    }

    public decimal WarehouseTotalValue
    {
        get => _warehouseTotalValue;
        private set => SetProperty(ref _warehouseTotalValue, value);
    }

    // ── Команды ──────────────────────────────────────────────────────────

    public ICommand RefreshCommand      { get; }
    public ICommand AddCommand          { get; }
    public ICommand EditCommand         { get; }
    public ICommand DeleteCommand       { get; }
    public ICommand InlineEditCommand   { get; }
    public ICommand InlineDeleteCommand { get; }

    // ── Загрузка ─────────────────────────────────────────────────────────

    public async Task LoadPartsAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            await using var db = new CarCareDbContext();
            var search = SearchText.Trim();

            var raw = await db.Parts
                .Where(p => string.IsNullOrEmpty(search) ||
                    p.Name.Contains(search) ||
                    (p.PartNumber != null && p.PartNumber.Contains(search)))
                .Where(p => !ShowLowStock || p.QuantityInStock <= 5)
                .OrderBy(p => p.Name)
                .ToListAsync();

            var rows = raw.Select(p => new PartRowDto(
                p.PartID,
                p.Name,
                p.PartNumber,
                p.QuantityInStock ?? 0,
                p.Price ?? 0m,
                (p.Price ?? 0m) * (p.QuantityInStock ?? 0))).ToList();

            Parts = new ObservableCollection<PartRowDto>(rows);

            // Обновляем статистические карточки — берём из всей БД (без фильтра)
            TotalPartsCount     = await db.Parts.CountAsync();
            LowStockCount       = await db.Parts.CountAsync(p => p.QuantityInStock <= 5);
            WarehouseTotalValue = await db.Parts.SumAsync(p => (p.Price ?? 0m) * (p.QuantityInStock ?? 0));

            StatusMessage = $"Позиций: {rows.Count}";
        }
        catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    // ── Действия ─────────────────────────────────────────────────────────

    private void ExecuteAdd()
    {
        var vm  = new PartEditViewModel(onSaved: async () => await LoadPartsAsync());
        var dlg = new PartEditDialog { DataContext = vm };
        vm.CloseRequested += (_, _) => dlg.Close();
        DialogHelper.SetOwner(dlg);
        dlg.ShowDialog();
        ToastHelper.Show("Позиция добавлена", ToastType.Success);
    }

    private void ExecuteEdit()
    {
        if (SelectedPart is null) return;
        OpenEditDialog(SelectedPart);
    }

    private void ExecuteInlineEdit(PartRowDto? row)
    {
        if (row is null) return;
        OpenEditDialog(row);
    }

    private void OpenEditDialog(PartRowDto row)
    {
        var vm  = new PartEditViewModel(row, onSaved: async () => await LoadPartsAsync());
        var dlg = new PartEditDialog { DataContext = vm };
        vm.CloseRequested += (_, _) => dlg.Close();
        DialogHelper.SetOwner(dlg);
        dlg.ShowDialog();
    }

    private async Task ExecuteDeleteAsync()
    {
        if (SelectedPart is null) return;
        await DeletePartAsync(SelectedPart);
    }

    private async Task ExecuteInlineDeleteAsync(PartRowDto? row)
    {
        if (row is null) return;
        await DeletePartAsync(row);
    }

    private async Task DeletePartAsync(PartRowDto row)
    {
        try
        {
            await using var db = new CarCareDbContext();
            bool inUse = await db.OrderParts.AnyAsync(op => op.PartID == row.PartID);
            if (inUse)
            {
                ToastHelper.Show($"Нельзя удалить «{row.Name}» — используется в заказах", ToastType.Warning);
                return;
            }

            var entity = await db.Parts.FindAsync(row.PartID);
            if (entity is not null)
            {
                db.Parts.Remove(entity);
                await db.SaveChangesAsync();
            }
            await LoadPartsAsync();
            ToastHelper.Show($"Позиция «{row.Name}» удалена", ToastType.Success);
        }
        catch (Exception ex)
        {
            ToastHelper.Show($"Ошибка удаления: {ex.Message}", ToastType.Error);
        }
    }
}
