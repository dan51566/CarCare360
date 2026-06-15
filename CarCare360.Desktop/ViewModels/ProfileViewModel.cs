using CarCare360.Desktop.Data;
using CarCare360.Desktop.Helpers;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CarCare360.Desktop.ViewModels;

public sealed class ProfileViewModel : BaseViewModel
{
    private string  _fullName          = string.Empty;
    private string  _phone             = string.Empty;
    private string  _email             = string.Empty;
    private string  _newLogin          = string.Empty;
    private string  _loginPassword     = string.Empty;
    private string  _oldPassword       = string.Empty;
    private string  _newPassword       = string.Empty;
    private string  _confirmPassword   = string.Empty;
    private bool    _isBusy;
    private string  _infoStatus        = string.Empty;
    private string  _loginStatus       = string.Empty;
    private string  _passwordStatus    = string.Empty;
    private ImageSource? _avatarImageSource;

    public ProfileViewModel()
    {
        _fullName = CurrentUser.FullName;
        _phone    = CurrentUser.Phone ?? string.Empty;
        _email    = CurrentUser.Email ?? string.Empty;
        _newLogin = CurrentUser.Login;

        SaveInfoCommand     = new RelayCommand(async () => await SaveInfoAsync(),     () => !IsBusy);
        SaveLoginCommand    = new RelayCommand(async () => await SaveLoginAsync(),    () => !IsBusy);
        SavePasswordCommand = new RelayCommand(async () => await SavePasswordAsync(), () => !IsBusy);
        UploadAvatarCommand = new RelayCommand(UploadAvatar);
        ClearAvatarCommand  = new RelayCommand(ClearAvatar, () => HasCustomAvatar);

        LoadAvatarImage();
    }

    // ── Личные данные ─────────────────────────────────────────────────────

    public string FullName
    {
        get => _fullName;
        set => SetProperty(ref _fullName, value);
    }

    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    // ── Изменить логин ────────────────────────────────────────────────────

    public string NewLogin
    {
        get => _newLogin;
        set => SetProperty(ref _newLogin, value);
    }

    public string LoginPassword
    {
        get => _loginPassword;
        set => SetProperty(ref _loginPassword, value);
    }

    // ── Изменить пароль ───────────────────────────────────────────────────

    public string OldPassword
    {
        get => _oldPassword;
        set => SetProperty(ref _oldPassword, value);
    }

    public string NewPassword
    {
        get => _newPassword;
        set => SetProperty(ref _newPassword, value);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
    }

    // ── Аватарка ─────────────────────────────────────────────────────────

    public ImageSource? AvatarImageSource
    {
        get => _avatarImageSource;
        private set
        {
            if (SetProperty(ref _avatarImageSource, value))
                OnPropertyChanged(nameof(HasCustomAvatar));
        }
    }

    public bool HasCustomAvatar => _avatarImageSource is not null;

    // ── Состояние ─────────────────────────────────────────────────────────

    public bool IsBusy
    {
        get => _isBusy;
        set { SetProperty(ref _isBusy, value); CommandManager.InvalidateRequerySuggested(); }
    }

    public string InfoStatus
    {
        get => _infoStatus;
        set => SetProperty(ref _infoStatus, value);
    }

    public string LoginStatus
    {
        get => _loginStatus;
        set => SetProperty(ref _loginStatus, value);
    }

    public string PasswordStatus
    {
        get => _passwordStatus;
        set => SetProperty(ref _passwordStatus, value);
    }

    // ── Отображение ───────────────────────────────────────────────────────

    public string CurrentLogin => CurrentUser.Login;
    public string RoleName     => CurrentUser.RoleName;

    public string Initials
    {
        get
        {
            var parts = (CurrentUser.FullName ?? "?")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant()
                : parts.Length == 1
                    ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
                    : "?";
        }
    }

    public SolidColorBrush AvatarBrush
    {
        get
        {
            var color = (CurrentUser.UserID % 6) switch
            {
                0 => Color.FromRgb(63,  81,  181),
                1 => Color.FromRgb(255, 107,   0),
                2 => Color.FromRgb(46,  125,  50),
                3 => Color.FromRgb(106,  27, 154),
                4 => Color.FromRgb(0,   105,  92),
                _ => Color.FromRgb(198,  40,  40),
            };
            return new SolidColorBrush(color);
        }
    }

    // ── Команды ──────────────────────────────────────────────────────────

    public ICommand SaveInfoCommand     { get; }
    public ICommand SaveLoginCommand    { get; }
    public ICommand SavePasswordCommand { get; }
    public ICommand UploadAvatarCommand { get; }
    public ICommand ClearAvatarCommand  { get; }

    // ── Аватарка: загрузка и очистка ─────────────────────────────────────

    private void LoadAvatarImage()
    {
        var path = UserAvatarStorage.GetAvatarPath(CurrentUser.UserID);
        if (path is not null && File.Exists(path))
            TrySetAvatar(path);
    }

    private void UploadAvatar()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title     = "Выберите фото профиля",
            Filter    = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Все файлы|*.*",
            Multiselect = false,
        };
        if (dialog.ShowDialog() != true) return;

        if (TrySetAvatar(dialog.FileName))
        {
            UserAvatarStorage.SaveAvatarPath(CurrentUser.UserID, dialog.FileName);
            CommandManager.InvalidateRequerySuggested();
            ToastHelper.Show("Фото обновлено", ToastType.Success);
        }
        else
        {
            InfoStatus = "Не удалось загрузить фото.";
        }
    }

    private void ClearAvatar()
    {
        AvatarImageSource = null;
        UserAvatarStorage.SaveAvatarPath(CurrentUser.UserID, null);
        CommandManager.InvalidateRequerySuggested();
        ToastHelper.Show("Фото удалено", ToastType.Info);
    }

    private bool TrySetAvatar(string path)
    {
        try
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.UriSource   = new Uri(path, UriKind.Absolute);
            img.EndInit();
            img.Freeze();
            AvatarImageSource = img;
            return true;
        }
        catch { return false; }
    }

    // ── Сохранить личные данные ───────────────────────────────────────────

    private async Task SaveInfoAsync()
    {
        if (string.IsNullOrWhiteSpace(FullName))
        { InfoStatus = "ФИО обязательно."; return; }

        IsBusy = true;
        InfoStatus = string.Empty;
        try
        {
            await using var db = new CarCareDbContext();
            var user = await db.Users.FindAsync(CurrentUser.UserID);
            if (user is null) { InfoStatus = "Пользователь не найден."; return; }

            user.FullName = FullName.Trim();
            user.Phone    = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim();
            user.Email    = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
            await db.SaveChangesAsync();

            CurrentUser.FullName = user.FullName;
            CurrentUser.Phone    = user.Phone;
            CurrentUser.Email    = user.Email;

            OnPropertyChanged(nameof(Initials));
            InfoStatus = "✓ Данные сохранены";
            ToastHelper.Show("Профиль обновлён", ToastType.Success);
        }
        catch (Exception ex) { InfoStatus = $"Ошибка: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    // ── Изменить логин ────────────────────────────────────────────────────

    private async Task SaveLoginAsync()
    {
        if (string.IsNullOrWhiteSpace(NewLogin))
        { LoginStatus = "Введите новый логин."; return; }
        if (string.IsNullOrWhiteSpace(LoginPassword))
        { LoginStatus = "Введите текущий пароль."; return; }

        IsBusy = true;
        LoginStatus = string.Empty;
        try
        {
            await using var db = new CarCareDbContext();
            var user = await db.Users.FindAsync(CurrentUser.UserID);
            if (user is null) { LoginStatus = "Пользователь не найден."; return; }

            if (!BCrypt.Net.BCrypt.Verify(LoginPassword, DatabaseSeeder.BytesToHash(user.PasswordHash)))
            { LoginStatus = "Неверный текущий пароль."; return; }

            bool taken = await db.Users.AnyAsync(
                u => u.Login == NewLogin.Trim() && u.UserID != CurrentUser.UserID);
            if (taken) { LoginStatus = "Этот логин уже занят."; return; }

            user.Login = NewLogin.Trim();
            await db.SaveChangesAsync();

            CurrentUser.Login = user.Login;
            LoginPassword = string.Empty;
            OnPropertyChanged(nameof(CurrentLogin));
            LoginStatus = "✓ Логин изменён";
            ToastHelper.Show("Логин обновлён", ToastType.Success);
        }
        catch (Exception ex) { LoginStatus = $"Ошибка: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    // ── Изменить пароль ───────────────────────────────────────────────────

    private async Task SavePasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(OldPassword))
        { PasswordStatus = "Введите текущий пароль."; return; }
        if (NewPassword.Length < 6)
        { PasswordStatus = "Новый пароль — минимум 6 символов."; return; }
        if (NewPassword != ConfirmPassword)
        { PasswordStatus = "Пароли не совпадают."; return; }

        IsBusy = true;
        PasswordStatus = string.Empty;
        try
        {
            await using var db = new CarCareDbContext();
            var user = await db.Users.FindAsync(CurrentUser.UserID);
            if (user is null) { PasswordStatus = "Пользователь не найден."; return; }

            if (!BCrypt.Net.BCrypt.Verify(OldPassword, DatabaseSeeder.BytesToHash(user.PasswordHash)))
            { PasswordStatus = "Неверный текущий пароль."; return; }

            user.PasswordHash = DatabaseSeeder.HashToBytes(
                BCrypt.Net.BCrypt.HashPassword(NewPassword, workFactor: 12));
            await db.SaveChangesAsync();

            OldPassword = ConfirmPassword = NewPassword = string.Empty;
            PasswordStatus = "✓ Пароль изменён";
            ToastHelper.Show("Пароль обновлён", ToastType.Success);
        }
        catch (Exception ex) { PasswordStatus = $"Ошибка: {ex.Message}"; }
        finally { IsBusy = false; }
    }
}
