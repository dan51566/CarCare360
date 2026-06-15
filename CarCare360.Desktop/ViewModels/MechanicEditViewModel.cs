using CarCare360.Desktop.Data;
using CarCare360.Desktop.Helpers;
using CarCare360.Desktop.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CarCare360.Desktop.ViewModels;

/// <summary>
/// ViewModel диалога добавления / редактирования механика.
/// Новый: создаёт User + Mechanic; редактирование: обновляет оба.
/// </summary>
public sealed class MechanicEditViewModel : BaseViewModel
{
    private readonly int?       _mechanicId; // null — новый
    private readonly int?       _userId;
    private readonly Func<Task> _onSaved;

    private string  _fullName          = string.Empty;
    private string  _login             = string.Empty;
    private string  _password          = string.Empty;
    private bool    _isActive          = true;
    private string  _hireDateStr       = string.Empty;
    private string  _qualificationLevel = string.Empty;
    private string  _errorMessage      = string.Empty;
    private bool    _isBusy;
    private Specialization? _selectedSpecialization;

    /// <summary>Конструктор для добавления нового механика.</summary>
    public MechanicEditViewModel(Func<Task> onSaved)
    {
        _mechanicId   = null;
        _userId       = null;
        _onSaved      = onSaved;
        IsNewMechanic = true;
        Title         = "Новый механик";
        Init();
    }

    /// <summary>Конструктор для редактирования существующего механика.</summary>
    public MechanicEditViewModel(MechanicRowDto dto, Func<Task> onSaved)
    {
        _mechanicId        = dto.MechanicID;
        _userId            = dto.UserID;
        _onSaved           = onSaved;
        IsNewMechanic      = false;
        Title              = "Редактировать механика";
        _fullName          = dto.FullName;
        _login             = dto.Login;
        _isActive          = dto.IsActive;
        _hireDateStr       = dto.HireDate?.ToString("dd.MM.yyyy") ?? string.Empty;
        _qualificationLevel = dto.QualificationLevel ?? string.Empty;
        Init();
    }

    private void Init()
    {
        SaveCommand   = new RelayCommand(async () => await SaveAsync(), CanSave);
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));
        _ = LoadSpecializationsAsync();
    }

    // ── Свойства ─────────────────────────────────────────────────────────

    public string Title         { get; }
    public bool   IsNewMechanic { get; }

    public string FullName
    {
        get => _fullName;
        set { SetProperty(ref _fullName, value); RaiseCanExecute(); }
    }

    public string Login
    {
        get => _login;
        set { SetProperty(ref _login, value); RaiseCanExecute(); }
    }

    public string Password
    {
        get => _password;
        set { SetProperty(ref _password, value); RaiseCanExecute(); }
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public string HireDateStr
    {
        get => _hireDateStr;
        set => SetProperty(ref _hireDateStr, value);
    }

    public string QualificationLevel
    {
        get => _qualificationLevel;
        set => SetProperty(ref _qualificationLevel, value);
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

    public Specialization? SelectedSpecialization
    {
        get => _selectedSpecialization;
        set => SetProperty(ref _selectedSpecialization, value);
    }

    public ObservableCollection<Specialization> Specializations { get; } = new();

    // ── Команды ──────────────────────────────────────────────────────────

    public ICommand SaveCommand   { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    public event EventHandler? CloseRequested;

    // ── Загрузка справочника ──────────────────────────────────────────────

    private async Task LoadSpecializationsAsync()
    {
        try
        {
            await using var db = new CarCareDbContext();
            var specs = await db.Specializations.OrderBy(s => s.Name).ToListAsync();
            Specializations.Clear();
            foreach (var s in specs) Specializations.Add(s);

            // При редактировании — подсвечиваем текущую специализацию
            if (!IsNewMechanic && _mechanicId.HasValue)
            {
                await using var db2 = new CarCareDbContext();
                var m = await db2.Mechanics.FindAsync(_mechanicId.Value);
                if (m?.SpecializationID.HasValue == true)
                    SelectedSpecialization = Specializations.FirstOrDefault(s => s.SpecID == m.SpecializationID);
            }
        }
        catch { /* тихо */ }
    }

    // ── Логика ───────────────────────────────────────────────────────────

    private bool CanSave()
    {
        if (IsBusy || string.IsNullOrWhiteSpace(FullName)) return false;
        if (IsNewMechanic && (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password)))
            return false;
        return true;
    }

    private void RaiseCanExecute() => CommandManager.InvalidateRequerySuggested();

    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        // Парсинг даты найма (необязательна)
        DateTime? hireDate = null;
        if (!string.IsNullOrWhiteSpace(HireDateStr))
        {
            if (!DateTime.TryParseExact(HireDateStr.Trim(),
                    ["dd.MM.yyyy", "yyyy-MM-dd"],
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var d))
            { ErrorMessage = "Неверный формат даты найма. Используйте ДД.ММ.ГГГГ."; return; }
            hireDate = d;
        }

        IsBusy = true;
        try
        {
            await using var db = new CarCareDbContext();

            if (IsNewMechanic)
            {
                // Проверка уникальности логина
                bool exists = await db.Users.AnyAsync(u => u.Login == Login.Trim());
                if (exists) { ErrorMessage = $"Логин «{Login.Trim()}» уже занят."; return; }

                // Получаем роль «Механик»
                var mechanicRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Механик");
                if (mechanicRole is null) { ErrorMessage = "Роль «Механик» не найдена в БД."; return; }

                // Создаём пользователя
                var hash = new byte[64];
                var ascii = System.Text.Encoding.ASCII.GetBytes(
                    BCrypt.Net.BCrypt.HashPassword(Password, workFactor: 12));
                Array.Copy(ascii, hash, Math.Min(ascii.Length, 64));

                var user = new User
                {
                    Login        = Login.Trim(),
                    PasswordHash = hash,
                    FullName     = FullName.Trim(),
                    RoleID       = mechanicRole.RoleID,
                    IsActive     = true,
                    CreatedAt    = DateTime.UtcNow
                };
                db.Users.Add(user);
                await db.SaveChangesAsync(); // получаем UserID

                // Создаём профиль механика
                db.Mechanics.Add(new Mechanic
                {
                    UserID             = user.UserID,
                    SpecializationID   = SelectedSpecialization?.SpecID,
                    HireDate           = hireDate,
                    QualificationLevel = string.IsNullOrWhiteSpace(QualificationLevel) ? null : QualificationLevel.Trim()
                });
                await db.SaveChangesAsync();
            }
            else
            {
                // Редактирование пользователя
                var user = await db.Users.FindAsync(_userId!.Value);
                if (user is not null)
                {
                    user.FullName = FullName.Trim();
                    user.IsActive = IsActive;
                }

                // Редактирование профиля механика
                var mech = await db.Mechanics.FindAsync(_mechanicId!.Value);
                if (mech is not null)
                {
                    mech.SpecializationID   = SelectedSpecialization?.SpecID;
                    mech.HireDate           = hireDate;
                    mech.QualificationLevel = string.IsNullOrWhiteSpace(QualificationLevel) ? null : QualificationLevel.Trim();
                }
                await db.SaveChangesAsync();
            }

            await _onSaved();
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) { ErrorMessage = ex.InnerException?.Message ?? ex.Message; }
        finally { IsBusy = false; }
    }
}
