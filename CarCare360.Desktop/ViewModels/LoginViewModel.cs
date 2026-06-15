using CarCare360.Desktop.Data;
using CarCare360.Desktop.Helpers;
using CarCare360.Desktop.Views;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CarCare360.Desktop.ViewModels;

/// <summary>
/// ViewModel окна авторизации.
/// Реализует:
///  — поиск пользователя по логину через EF Core;
///  — проверку BCrypt-хеша (PasswordHash хранится как BINARY(64));
///  — in-memory счётчик неудачных попыток (БД не хранит FailedLoginCount);
///  — блокировку учётной записи (IsActive = false) после 3 провалов;
///  — ролевой вход с заполнением CurrentUser.
/// </summary>
public sealed class LoginViewModel : BaseViewModel
{
    /// <summary>
    /// Максимальное количество неудачных попыток до блокировки.
    /// Счётчик хранится в памяти — сбрасывается при перезапуске приложения.
    /// </summary>
    private const int MaxFailedAttempts = 3;

    /// <summary>
    /// Глобальный in-memory счётчик неудачных попыток.
    /// Ключ — логин (в нижнем регистре), значение — количество провалов текущей сессии.
    /// </summary>
    private static readonly ConcurrentDictionary<string, int> FailedAttempts = new();

    private string _login = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isLoading;
    private bool _rememberMe;

    public LoginViewModel()
    {
        // Async void допустим для ICommand.Execute в WPF
        LoginCommand = new RelayCommand(
            async () => await ExecuteLoginAsync(),
            () => !IsLoading);
        RegisterCommand = new RelayCommand(OpenRegistration);

        // Восстанавливаем сохранённый логин
        var saved = RememberLoginHelper.Load();
        if (saved is not null)
        {
            _login      = saved;
            _rememberMe = true;
        }
    }

    /// <summary>Логин пользователя.</summary>
    public string Login
    {
        get => _login;
        set => SetProperty(ref _login, value);
    }

    /// <summary>Пароль (синхронизируется через PasswordBoxHelper).</summary>
    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    /// <summary>Сообщение об ошибке (пусто — если ошибки нет).</summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>Запомнить логин для следующего входа.</summary>
    public bool RememberMe
    {
        get => _rememberMe;
        set
        {
            SetProperty(ref _rememberMe, value);
            if (!value) RememberLoginHelper.Clear();
        }
    }

    /// <summary>Флаг загрузки (блокирует кнопку «Войти» во время запроса к БД).</summary>
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            SetProperty(ref _isLoading, value);
            // Сообщаем WPF пересчитать CanExecute
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>Команда «Войти».</summary>
    public ICommand LoginCommand { get; }

    /// <summary>Команда «Зарегистрироваться».</summary>
    public ICommand RegisterCommand { get; }

    /// <summary>
    /// Основная логика авторизации — выполняется асинхронно.
    /// </summary>
    private async Task ExecuteLoginAsync()
    {
        ErrorMessage = string.Empty;

        // ── Базовая валидация ──────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Введите логин и пароль";
            return;
        }

        IsLoading = true;

        try
        {
            await using var db = new CarCareDbContext();

            // ── Поиск пользователя (параметризованный запрос EF) ──────────
            var loginKey = Login.Trim();
            var user = await db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Login == loginKey);

            // Пользователь не найден — не раскрываем причину
            if (user is null)
            {
                ErrorMessage = "Неверный логин или пароль";
                return;
            }

            // Учётная запись заблокирована
            if (user.IsActive == false)
            {
                ErrorMessage = "Учётная запись заблокирована. Обратитесь к администратору.";
                return;
            }

            // ── Проверка пароля через BCrypt ───────────────────────────────
            // PasswordHash: BINARY(64) → конвертируем обратно в BCrypt-строку
            var hashString = DatabaseSeeder.BytesToHash(user.PasswordHash);
            bool passwordValid = BCrypt.Net.BCrypt.Verify(Password, hashString);

            if (!passwordValid)
            {
                // Увеличиваем in-memory счётчик для этого логина
                var count = FailedAttempts.AddOrUpdate(
                    loginKey.ToLowerInvariant(),
                    1,
                    (_, old) => old + 1);

                if (count >= MaxFailedAttempts)
                {
                    // Блокируем учётную запись в БД
                    user.IsActive = false;
                    await db.SaveChangesAsync();
                    // Очищаем счётчик — учётная запись заблокирована, дальше не считаем
                    FailedAttempts.TryRemove(loginKey.ToLowerInvariant(), out _);
                    ErrorMessage = "Учётная запись заблокирована после 3 неудачных попыток. " +
                                   "Обратитесь к администратору.";
                }
                else
                {
                    int remaining = MaxFailedAttempts - (int)count;
                    ErrorMessage = $"Неверный логин или пароль. " +
                                   $"Осталось попыток: {remaining}.";
                }

                return;
            }

            // ── Успешный вход ──────────────────────────────────────────────
            // Сбрасываем счётчик неудачных попыток
            FailedAttempts.TryRemove(loginKey.ToLowerInvariant(), out _);

            // Сохраняем логин, если выбран чекбокс «Запомнить меня»
            if (RememberMe)
                RememberLoginHelper.Save(loginKey);
            else
                RememberLoginHelper.Clear();

            // Заполняем глобальный контекст пользователя
            CurrentUser.UserID   = user.UserID;
            CurrentUser.FullName = user.FullName;
            CurrentUser.RoleName = user.Role!.Name;
            CurrentUser.Login    = user.Login;
            CurrentUser.Phone    = user.Phone;
            CurrentUser.Email    = user.Email;

            OpenMainWindow();
            CloseLoginWindow();
        }
        catch (Exception ex)
        {
            // Собираем полную цепочку исключений для отладки
            var inner = ex.InnerException;
            var msg = ex.Message;
            if (inner != null) msg += $"\n[{inner.GetType().Name}] {inner.Message}";
            if (inner?.InnerException != null) msg += $"\n{inner.InnerException.Message}";

            // Пишем стек в лог рядом с exe
            try
            {
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "login_error.log"),
                    $"[{DateTime.Now:HH:mm:ss}] {ex.GetType().Name}: {ex.Message}\n" +
                    (inner != null ? $"Inner: {inner.Message}\n" : "") +
                    $"Stack:\n{ex.StackTrace}");
            }
            catch { /* ignore */ }

            ErrorMessage = $"Ошибка: {msg}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Открывает нужное окно в зависимости от роли: клиенты → ClientWindow, остальные → MainWindow.</summary>
    private static void OpenMainWindow()
    {
        if (string.Equals(CurrentUser.RoleName, "Клиент", StringComparison.OrdinalIgnoreCase))
        {
            var client = new Views.ClientWindow
            {
                DataContext = new ClientMainViewModel()
            };
            client.Show();
            Application.Current.MainWindow = client;
        }
        else
        {
            var main = new Views.MainWindow
            {
                DataContext = new MainViewModel(CurrentUser.RoleName)
            };
            main.Show();
            Application.Current.MainWindow = main;
        }
    }

    /// <summary>Закрывает окно авторизации.</summary>
    private static void CloseLoginWindow()
    {
        var login = Application.Current.Windows.OfType<LoginWindow>().FirstOrDefault();
        login?.Close();
    }

    /// <summary>Открывает окно регистрации.</summary>
    private static void OpenRegistration()
    {
        var vm = new RegistrationViewModel();
        var window = new RegistrationWindow { DataContext = vm };
        vm.CloseRequested += (_, _) => window.Close();
        vm.RegistrationSucceeded += (_, _) =>
        {
            window.Close();
            var msg = vm.IsMechanic
                ? "Заявка на регистрацию механика отправлена. Ожидайте подтверждения администратора."
                : "Регистрация прошла успешно! Теперь вы можете войти.";
            System.Windows.MessageBox.Show(msg, "Регистрация",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        };
        window.ShowDialog();
    }
}
