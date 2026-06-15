using CarCare360.Desktop.Data;
using CarCare360.Desktop.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Windows.Input;

namespace CarCare360.Desktop.ViewModels;

/// <summary>
/// ViewModel диалога добавления / редактирования клиента.
///
/// Режим «Новый клиент»:
///   — принимает FullName, Login, Password, Phone, Email;
///   — вызывает хранимую процедуру RegisterClient (с BCrypt-хешем).
///
/// Режим «Редактирование»:
///   — принимает существующий <see cref="ClientRowDto"/>;
///   — обновляет только FullName, Phone, Email (Login и пароль не меняются).
/// </summary>
public sealed class ClientEditViewModel : BaseViewModel
{
    private readonly int?      _userId;   // null — новый клиент
    private readonly Func<Task> _onSaved;

    private string _fullName     = string.Empty;
    private string _login        = string.Empty;
    private string _phone        = string.Empty;
    private string _email        = string.Empty;
    private string _password     = string.Empty;
    private string _errorMessage = string.Empty;
    private bool   _isBusy;

    // ── Конструктор: новый клиент ─────────────────────────────────────────

    /// <param name="onSaved">Callback после успешного сохранения — обновляет список клиентов.</param>
    public ClientEditViewModel(Func<Task> onSaved)
    {
        _userId    = null;
        _onSaved   = onSaved;
        IsNewClient = true;
        SaveCommand   = new RelayCommand(async () => await SaveAsync(), CanSave);
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));
    }

    // ── Конструктор: редактирование ───────────────────────────────────────

    /// <param name="client">DTO существующего клиента.</param>
    /// <param name="onSaved">Callback после успешного сохранения.</param>
    public ClientEditViewModel(ClientRowDto client, Func<Task> onSaved)
    {
        _userId    = client.UserID;
        _onSaved   = onSaved;
        IsNewClient = false;

        _fullName = client.FullName;
        _login    = client.Login;
        _phone    = client.Phone ?? string.Empty;
        _email    = client.Email ?? string.Empty;

        SaveCommand   = new RelayCommand(async () => await SaveAsync(), CanSave);
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));
    }

    // ── Свойства ─────────────────────────────────────────────────────────

    /// <summary>true — создание нового клиента; false — редактирование.</summary>
    public bool   IsNewClient { get; }

    /// <summary>Заголовок диалога.</summary>
    public string Title => IsNewClient ? "Новый клиент" : "Редактировать клиента";

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

    /// <summary>Пароль (только для нового клиента). Синхронизируется из PasswordBox в code-behind.</summary>
    public string Password
    {
        get => _password;
        set { SetProperty(ref _password, value); RaiseCanExecute(); }
    }

    /// <summary>Текст ошибки (пусто — нет ошибки).</summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>Блокирует кнопку «Сохранить» во время выполнения запроса.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        set { SetProperty(ref _isBusy, value); RaiseCanExecute(); }
    }

    // ── Команды ──────────────────────────────────────────────────────────

    public ICommand SaveCommand   { get; }
    public ICommand CancelCommand { get; }

    /// <summary>Сигнал для code-behind диалога — закрыть окно.</summary>
    public event EventHandler? CloseRequested;

    // ── Логика ───────────────────────────────────────────────────────────

    private bool CanSave() =>
        !IsBusy &&
        !string.IsNullOrWhiteSpace(FullName) &&
        !string.IsNullOrWhiteSpace(Login) &&
        (!IsNewClient || !string.IsNullOrWhiteSpace(Password));

    private void RaiseCanExecute() => CommandManager.InvalidateRequerySuggested();

    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        // Дополнительная проверка перед сохранением
        if (string.IsNullOrWhiteSpace(FullName))   { ErrorMessage = "Введите полное имя клиента."; return; }
        if (string.IsNullOrWhiteSpace(Login))       { ErrorMessage = "Введите логин."; return; }
        if (IsNewClient && string.IsNullOrWhiteSpace(Password)) { ErrorMessage = "Введите пароль."; return; }

        IsBusy = true;
        try
        {
            await using var db = new CarCareDbContext();

            if (IsNewClient)
            {
                // ── Проверка уникальности логина ──────────────────────────
                bool exists = await db.Users.AnyAsync(u => u.Login == Login.Trim());
                if (exists)
                {
                    ErrorMessage = "Пользователь с таким логином уже существует.";
                    return;
                }

                // ── Хеш пароля: BCrypt → BINARY(64) ──────────────────────
                var hash = DatabaseSeeder.HashToBytes(
                    BCrypt.Net.BCrypt.HashPassword(Password.Trim()));

                // ── Вызов хранимой процедуры RegisterClient ───────────────
                var cs = db.Database.GetConnectionString()!;
                await using var conn = new SqlConnection(cs);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand("RegisterClient", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Login",    Login.Trim());
                var ph = cmd.Parameters.Add("@PasswordHash", SqlDbType.Binary, 64);
                ph.Value = hash;
                cmd.Parameters.AddWithValue("@FullName", FullName.Trim());
                cmd.Parameters.AddWithValue("@Email",
                    string.IsNullOrWhiteSpace(Email) ? DBNull.Value : (object)Email.Trim());
                cmd.Parameters.AddWithValue("@Phone",
                    string.IsNullOrWhiteSpace(Phone) ? DBNull.Value : (object)Phone.Trim());
                await cmd.ExecuteNonQueryAsync();
            }
            else
            {
                // ── Редактирование существующего клиента ──────────────────
                var user = await db.Users.FindAsync(_userId!.Value);
                if (user is null) { ErrorMessage = "Клиент не найден в базе данных."; return; }

                user.FullName = FullName.Trim();
                user.Phone    = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim();
                user.Email    = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();

                await db.SaveChangesAsync();
            }

            // Уведомляем список и закрываем диалог
            await _onSaved();
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException;
            ErrorMessage = inner?.Message ?? ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
