using CarCare360.Desktop.Data;
using CarCare360.Desktop.Helpers;
using CarCare360.Desktop.Models;
using System.Windows.Input;

namespace CarCare360.Desktop.ViewModels;

/// <summary>
/// ViewModel диалога добавления / редактирования запчасти на складе.
/// </summary>
public sealed class PartEditViewModel : BaseViewModel
{
    private readonly int?        _partId;   // null — новая запчасть
    private readonly Func<Task>  _onSaved;

    private string _name        = string.Empty;
    private string _partNumber  = string.Empty;
    private string _stockText   = "0";
    private string _priceText   = "0";
    private string _errorMessage = string.Empty;
    private bool   _isBusy;

    /// <summary>Конструктор для добавления новой запчасти.</summary>
    public PartEditViewModel(Func<Task> onSaved)
    {
        _partId  = null;
        _onSaved = onSaved;
        Title    = "Новая запчасть";
        Init();
    }

    /// <summary>Конструктор для редактирования существующей запчасти.</summary>
    public PartEditViewModel(PartRowDto part, Func<Task> onSaved)
    {
        _partId      = part.PartID;
        _onSaved     = onSaved;
        Title        = "Редактировать запчасть";
        _name        = part.Name;
        _partNumber  = part.PartNumber ?? string.Empty;
        _stockText   = part.Stock.ToString();
        _priceText   = part.Price.ToString("F2");
        Init();
    }

    private void Init()
    {
        SaveCommand   = new RelayCommand(async () => await SaveAsync(), CanSave);
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));
    }

    // ── Свойства ─────────────────────────────────────────────────────────

    public string Title { get; }

    public string Name
    {
        get => _name;
        set { SetProperty(ref _name, value); RaiseCanExecute(); }
    }

    public string PartNumber
    {
        get => _partNumber;
        set => SetProperty(ref _partNumber, value);
    }

    public string StockText
    {
        get => _stockText;
        set => SetProperty(ref _stockText, value);
    }

    public string PriceText
    {
        get => _priceText;
        set => SetProperty(ref _priceText, value);
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

    public ICommand SaveCommand   { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    public event EventHandler? CloseRequested;

    // ── Логика ───────────────────────────────────────────────────────────

    private bool CanSave() => !IsBusy && !string.IsNullOrWhiteSpace(Name);
    private void RaiseCanExecute() => CommandManager.InvalidateRequerySuggested();

    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Name))
        { ErrorMessage = "Введите название запчасти."; return; }

        if (!int.TryParse(StockText.Trim(), out int stock) || stock < 0)
        { ErrorMessage = "Остаток должен быть целым числом ≥ 0."; return; }

        if (!decimal.TryParse(PriceText.Trim().Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal price) || price < 0)
        { ErrorMessage = "Цена должна быть числом ≥ 0."; return; }

        IsBusy = true;
        try
        {
            await using var db = new CarCareDbContext();

            if (_partId is null)
            {
                // Новая запчасть
                db.Parts.Add(new Part
                {
                    Name            = Name.Trim(),
                    PartNumber      = string.IsNullOrWhiteSpace(PartNumber) ? null : PartNumber.Trim(),
                    QuantityInStock = stock,
                    Price           = price
                });
            }
            else
            {
                // Редактирование
                var entity = await db.Parts.FindAsync(_partId.Value);
                if (entity is null) { ErrorMessage = "Запчасть не найдена."; return; }
                entity.Name            = Name.Trim();
                entity.PartNumber      = string.IsNullOrWhiteSpace(PartNumber) ? null : PartNumber.Trim();
                entity.QuantityInStock = stock;
                entity.Price           = price;
            }

            await db.SaveChangesAsync();
            await _onSaved();
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) { ErrorMessage = ex.InnerException?.Message ?? ex.Message; }
        finally { IsBusy = false; }
    }
}
