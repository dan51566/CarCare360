using CarCare360.Desktop.Data;
using CarCare360.Desktop.Helpers;
using CarCare360.Desktop.Models;
using CarCare360.Desktop.Views;
using Microsoft.EntityFrameworkCore;
using System.Windows.Input;

namespace CarCare360.Desktop.ViewModels;

public sealed class RegistrationViewModel : BaseViewModel
{
    private string _fullName        = string.Empty;
    private string _login           = string.Empty;
    private string _password        = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _phone           = string.Empty;
    private bool   _isMechanic;
    private string _errorMessage    = string.Empty;
    private bool   _isBusy;

    public RegistrationViewModel()
    {
        RegisterCommand = new RelayCommand(async () => await RegisterAsync(), CanRegister);
        BackCommand     = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));
    }

    public string FullName
    {
        get => _fullName;
        set { SetProperty(ref _fullName, value); Raise(); }
    }
    public string Login
    {
        get => _login;
        set { SetProperty(ref _login, value); Raise(); }
    }
    public string Password
    {
        get => _password;
        set { SetProperty(ref _password, value); Raise(); }
    }
    public string ConfirmPassword
    {
        get => _confirmPassword;
        set { SetProperty(ref _confirmPassword, value); Raise(); }
    }
    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }
    public bool IsMechanic
    {
        get => _isMechanic;
        set { SetProperty(ref _isMechanic, value); OnPropertyChanged(nameof(RoleInfo)); }
    }
    public string RoleInfo => IsMechanic
        ? "Заявка будет рассмотрена администратором"
        : "Войдёте сразу после регистрации";
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }
    public bool IsBusy
    {
        get => _isBusy;
        set { SetProperty(ref _isBusy, value); Raise(); }
    }

    public ICommand RegisterCommand { get; }
    public ICommand BackCommand     { get; }
    public event EventHandler? CloseRequested;
    public event EventHandler? RegistrationSucceeded;

    private bool CanRegister() =>
        !IsBusy &&
        !string.IsNullOrWhiteSpace(FullName) &&
        !string.IsNullOrWhiteSpace(Login) &&
        Password.Length >= 6;

    private void Raise() => CommandManager.InvalidateRequerySuggested();

    private async Task RegisterAsync()
    {
        ErrorMessage = string.Empty;
        if (Password != ConfirmPassword) { ErrorMessage = "Пароли не совпадают."; return; }
        if (Password.Length < 6) { ErrorMessage = "Пароль — не менее 6 символов."; return; }

        IsBusy = true;
        try
        {
            await using var db = new CarCareDbContext();
            if (await db.Users.AnyAsync(u => u.Login == Login.Trim()))
            { ErrorMessage = $"Логин «{Login.Trim()}» уже занят."; return; }

            var roleName = IsMechanic ? "Механик" : "Клиент";
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role is null) { ErrorMessage = $"Роль «{roleName}» не найдена."; return; }

            var hashStr   = BCrypt.Net.BCrypt.HashPassword(Password, workFactor: 12);
            var hashBytes = new byte[64];
            var ascii     = System.Text.Encoding.ASCII.GetBytes(hashStr);
            Array.Copy(ascii, hashBytes, Math.Min(ascii.Length, 64));

            var user = new User
            {
                Login        = Login.Trim(),
                PasswordHash = hashBytes,
                FullName     = FullName.Trim(),
                Phone        = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                RoleID       = role.RoleID,
                IsActive     = !IsMechanic,
                CreatedAt    = DateTime.UtcNow,
                IsDeleted    = false
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            if (IsMechanic)
            {
                db.Mechanics.Add(new Mechanic { UserID = user.UserID, HireDate = null });
                await db.SaveChangesAsync();
            }

            RegistrationSucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) { ErrorMessage = ex.InnerException?.Message ?? ex.Message; }
        finally { IsBusy = false; }
    }
}
