using CarCare360.Desktop.Data;
using CarCare360.Desktop.Helpers;
using CarCare360.Desktop.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CarCare360.Desktop.ViewModels;

public record SpecRefDto(int SpecID, string Name);

/// <summary>ViewModel вкладки «Специализации» в разделе Справочники.</summary>
public sealed class SpecializationsRefViewModel : BaseViewModel
{
    private SpecRefDto? _selectedItem;
    private bool   _isEditing;
    private string _searchText    = string.Empty;
    private string _editName      = string.Empty;
    private string _statusMessage = string.Empty;
    private int?   _editingId;

    public SpecializationsRefViewModel()
    {
        RefreshCommand    = new RelayCommand(async () => await LoadAsync());
        ShowAddCommand    = new RelayCommand(StartAdd);
        ShowEditCommand   = new RelayCommand(StartEdit, () => SelectedItem is not null);
        SaveEditCommand   = new RelayCommand(async () => await SaveAsync(),
                               () => !string.IsNullOrWhiteSpace(EditName));
        CancelEditCommand = new RelayCommand(() => IsEditing = false);
        DeleteCommand     = new RelayCommand(async () => await DeleteAsync(),
                               () => SelectedItem is not null);
        _ = LoadAsync();
    }

    public ObservableCollection<SpecRefDto> Items { get; } = new();

    public string SearchText
    {
        get => _searchText;
        set { if (SetProperty(ref _searchText, value)) _ = LoadAsync(); }
    }

    public SpecRefDto? SelectedItem
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
        set { SetProperty(ref _editName, value); CommandManager.InvalidateRequerySuggested(); }
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
            var list = await db.Specializations
                .Where(s => string.IsNullOrEmpty(search) || s.Name.Contains(search))
                .OrderBy(s => s.Name)
                .ToListAsync();
            Items.Clear();
            foreach (var s in list)
                Items.Add(new SpecRefDto(s.SpecID, s.Name));
            StatusMessage = $"Специализаций: {Items.Count}";
        }
        catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
    }

    private void StartAdd()  { _editingId = null; EditName = string.Empty; IsEditing = true; }
    private void StartEdit()
    {
        if (SelectedItem is null) return;
        _editingId = SelectedItem.SpecID;
        EditName   = SelectedItem.Name;
        IsEditing  = true;
    }

    private async Task SaveAsync()
    {
        try
        {
            await using var db = new CarCareDbContext();
            if (_editingId is null)
                db.Specializations.Add(new Specialization { Name = EditName.Trim() });
            else
            {
                var e = await db.Specializations.FindAsync(_editingId.Value);
                if (e is not null) e.Name = EditName.Trim();
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
            bool inUse = await db.Mechanics.AnyAsync(m => m.SpecializationID == SelectedItem.SpecID);
            if (inUse) { StatusMessage = $"Нельзя удалить «{SelectedItem.Name}» — назначена механикам."; return; }
            var e = await db.Specializations.FindAsync(SelectedItem.SpecID);
            if (e is not null) { db.Specializations.Remove(e); await db.SaveChangesAsync(); }
            await LoadAsync();
        }
        catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
    }
}
