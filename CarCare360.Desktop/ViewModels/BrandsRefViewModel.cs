using CarCare360.Desktop.Data;
using CarCare360.Desktop.Helpers;
using CarCare360.Desktop.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CarCare360.Desktop.ViewModels;

public record BrandRefDto(int BrandID, string Name, string? Country);

/// <summary>ViewModel вкладки «Марки и модели» в разделе Справочники.</summary>
public sealed class BrandsRefViewModel : BaseViewModel
{
    // ── Бренды ───────────────────────────────────────────────────────────
    private BrandRefDto? _selectedBrand;
    private bool   _isEditingBrand;
    private string _brandSearchText  = string.Empty;
    private string _editBrandName    = string.Empty;
    private string _editBrandCountry = string.Empty;
    private int?   _editingBrandId;

    // ── Модели ────────────────────────────────────────────────────────────
    private CarModel? _selectedModel;
    private bool   _isEditingModel;
    private string _editModelName = string.Empty;
    private int?   _editingModelId;

    private string _statusMessage = string.Empty;

    public BrandsRefViewModel()
    {
        // Бренды
        BrandRefreshCommand    = new RelayCommand(async () => await LoadBrandsAsync());
        BrandShowAddCommand    = new RelayCommand(StartAddBrand);
        BrandShowEditCommand   = new RelayCommand(StartEditBrand, () => SelectedBrand is not null);
        BrandSaveCommand       = new RelayCommand(async () => await SaveBrandAsync(),
                                    () => !string.IsNullOrWhiteSpace(EditBrandName));
        BrandCancelCommand     = new RelayCommand(() => IsEditingBrand = false);
        BrandDeleteCommand     = new RelayCommand(async () => await DeleteBrandAsync(),
                                    () => SelectedBrand is not null);

        // Модели
        ModelShowAddCommand    = new RelayCommand(StartAddModel, () => SelectedBrand is not null);
        ModelShowEditCommand   = new RelayCommand(StartEditModel, () => SelectedModel is not null);
        ModelSaveCommand       = new RelayCommand(async () => await SaveModelAsync(),
                                    () => !string.IsNullOrWhiteSpace(EditModelName) && SelectedBrand is not null);
        ModelCancelCommand     = new RelayCommand(() => IsEditingModel = false);
        ModelDeleteCommand     = new RelayCommand(async () => await DeleteModelAsync(),
                                    () => SelectedModel is not null);

        _ = LoadBrandsAsync();
    }

    // ── Коллекции ─────────────────────────────────────────────────────────
    public ObservableCollection<BrandRefDto> Brands     { get; } = new();
    public ObservableCollection<CarModel>    BrandModels { get; } = new();

    // ── Бренды ───────────────────────────────────────────────────────────
    public string BrandSearchText
    {
        get => _brandSearchText;
        set { if (SetProperty(ref _brandSearchText, value)) _ = LoadBrandsAsync(); }
    }

    public BrandRefDto? SelectedBrand
    {
        get => _selectedBrand;
        set
        {
            if (SetProperty(ref _selectedBrand, value))
            {
                CommandManager.InvalidateRequerySuggested();
                _ = LoadModelsAsync();
            }
        }
    }

    public bool IsEditingBrand
    {
        get => _isEditingBrand;
        set => SetProperty(ref _isEditingBrand, value);
    }

    public string EditBrandName
    {
        get => _editBrandName;
        set { SetProperty(ref _editBrandName, value); CommandManager.InvalidateRequerySuggested(); }
    }

    public string EditBrandCountry
    {
        get => _editBrandCountry;
        set => SetProperty(ref _editBrandCountry, value);
    }

    // ── Модели ────────────────────────────────────────────────────────────
    public CarModel? SelectedModel
    {
        get => _selectedModel;
        set { SetProperty(ref _selectedModel, value); CommandManager.InvalidateRequerySuggested(); }
    }

    public bool IsEditingModel
    {
        get => _isEditingModel;
        set => SetProperty(ref _isEditingModel, value);
    }

    public string EditModelName
    {
        get => _editModelName;
        set { SetProperty(ref _editModelName, value); CommandManager.InvalidateRequerySuggested(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    // ── Команды ──────────────────────────────────────────────────────────
    public ICommand BrandRefreshCommand  { get; }
    public ICommand BrandShowAddCommand  { get; }
    public ICommand BrandShowEditCommand { get; }
    public ICommand BrandSaveCommand     { get; }
    public ICommand BrandCancelCommand   { get; }
    public ICommand BrandDeleteCommand   { get; }

    public ICommand ModelShowAddCommand  { get; }
    public ICommand ModelShowEditCommand { get; }
    public ICommand ModelSaveCommand     { get; }
    public ICommand ModelCancelCommand   { get; }
    public ICommand ModelDeleteCommand   { get; }

    // ── Загрузка ─────────────────────────────────────────────────────────
    public async Task LoadBrandsAsync()
    {
        try
        {
            await using var db = new CarCareDbContext();
            var search = BrandSearchText.Trim();
            var list = await db.CarBrands
                .Where(b => string.IsNullOrEmpty(search) || b.Name.Contains(search))
                .OrderBy(b => b.Name)
                .ToListAsync();
            Brands.Clear();
            foreach (var b in list)
                Brands.Add(new BrandRefDto(b.BrandID, b.Name, b.Country));
        }
        catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
    }

    private async Task LoadModelsAsync()
    {
        BrandModels.Clear();
        if (SelectedBrand is null) return;
        try
        {
            await using var db = new CarCareDbContext();
            var list = await db.CarModels
                .Where(m => m.BrandID == SelectedBrand.BrandID)
                .OrderBy(m => m.Name)
                .ToListAsync();
            foreach (var m in list) BrandModels.Add(m);
        }
        catch { /* тихо */ }
    }

    // ── Бренды: CRUD ─────────────────────────────────────────────────────
    private void StartAddBrand()
    {
        _editingBrandId  = null;
        EditBrandName    = string.Empty;
        EditBrandCountry = string.Empty;
        IsEditingBrand   = true;
    }

    private void StartEditBrand()
    {
        if (SelectedBrand is null) return;
        _editingBrandId  = SelectedBrand.BrandID;
        EditBrandName    = SelectedBrand.Name;
        EditBrandCountry = SelectedBrand.Country ?? string.Empty;
        IsEditingBrand   = true;
    }

    private async Task SaveBrandAsync()
    {
        try
        {
            await using var db = new CarCareDbContext();
            if (_editingBrandId is null)
                db.CarBrands.Add(new CarBrand
                {
                    Name    = EditBrandName.Trim(),
                    Country = string.IsNullOrWhiteSpace(EditBrandCountry) ? null : EditBrandCountry.Trim()
                });
            else
            {
                var e = await db.CarBrands.FindAsync(_editingBrandId.Value);
                if (e is not null)
                {
                    e.Name    = EditBrandName.Trim();
                    e.Country = string.IsNullOrWhiteSpace(EditBrandCountry) ? null : EditBrandCountry.Trim();
                }
            }
            await db.SaveChangesAsync();
            IsEditingBrand = false;
            await LoadBrandsAsync();
        }
        catch (Exception ex) { StatusMessage = $"Ошибка: {ex.InnerException?.Message ?? ex.Message}"; }
    }

    private async Task DeleteBrandAsync()
    {
        if (SelectedBrand is null) return;
        try
        {
            await using var db = new CarCareDbContext();
            bool hasModels = await db.CarModels.AnyAsync(m => m.BrandID == SelectedBrand.BrandID);
            if (hasModels) { StatusMessage = $"Нельзя удалить «{SelectedBrand.Name}» — есть модели."; return; }
            var e = await db.CarBrands.FindAsync(SelectedBrand.BrandID);
            if (e is not null) { db.CarBrands.Remove(e); await db.SaveChangesAsync(); }
            await LoadBrandsAsync();
        }
        catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
    }

    // ── Модели: CRUD ──────────────────────────────────────────────────────
    private void StartAddModel()
    {
        _editingModelId = null;
        EditModelName   = string.Empty;
        IsEditingModel  = true;
    }

    private void StartEditModel()
    {
        if (SelectedModel is null) return;
        _editingModelId = SelectedModel.ModelID;
        EditModelName   = SelectedModel.Name;
        IsEditingModel  = true;
    }

    private async Task SaveModelAsync()
    {
        if (SelectedBrand is null) return;
        try
        {
            await using var db = new CarCareDbContext();
            if (_editingModelId is null)
                db.CarModels.Add(new CarModel { BrandID = SelectedBrand.BrandID, Name = EditModelName.Trim() });
            else
            {
                var e = await db.CarModels.FindAsync(_editingModelId.Value);
                if (e is not null) e.Name = EditModelName.Trim();
            }
            await db.SaveChangesAsync();
            IsEditingModel = false;
            await LoadModelsAsync();
        }
        catch (Exception ex) { StatusMessage = $"Ошибка: {ex.InnerException?.Message ?? ex.Message}"; }
    }

    private async Task DeleteModelAsync()
    {
        if (SelectedModel is null) return;
        try
        {
            await using var db = new CarCareDbContext();
            bool inUse = await db.Cars.AnyAsync(c => c.ModelID == SelectedModel.ModelID);
            if (inUse) { StatusMessage = $"Нельзя удалить «{SelectedModel.Name}» — есть автомобили."; return; }
            var e = await db.CarModels.FindAsync(SelectedModel.ModelID);
            if (e is not null) { db.CarModels.Remove(e); await db.SaveChangesAsync(); }
            await LoadModelsAsync();
        }
        catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
    }
}
