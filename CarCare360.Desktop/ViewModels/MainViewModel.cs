using CarCare360.Desktop.Helpers;
using CarCare360.Desktop.Views;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CarCare360.Desktop.ViewModels;

public sealed class MainViewModel : BaseViewModel
{
    public const string MechanicRoleName = "Механик";

    private object?      _currentView;
    private ImageSource? _sidebarAvatarSource;

    public MainViewModel(string roleName)
    {
        RoleName = roleName;

        CurrentView = new ClientsView();

        NavigateClientsCommand    = new RelayCommand(() => CurrentView = new ClientsView());
        NavigateCarsCommand       = new RelayCommand(() => CurrentView = new CarsView());
        NavigateOrdersCommand     = new RelayCommand(() => CurrentView = new OrdersView());
        NavigateWarehouseCommand  = new RelayCommand(() => CurrentView = new WarehouseView());
        NavigateMechanicsCommand  = new RelayCommand(() => CurrentView = new MechanicsView());
        NavigateReferencesCommand = new RelayCommand(() => CurrentView = new ReferencesView());
        NavigateAuditCommand      = new RelayCommand(() => CurrentView = new AuditView());
        NavigateReportsCommand    = new RelayCommand(() => CurrentView = new ReportsView());
        NavigateProfileCommand    = new RelayCommand(() => CurrentView = new ProfileView());
        LogoutCommand             = new RelayCommand(ExecuteLogout);

        // Load sidebar avatar and listen for changes made in ProfileView
        LoadSidebarAvatar();
        UserAvatarStorage.AvatarChanged += OnAvatarChanged;
    }

    // ── Свойства ─────────────────────────────────────────────────────────

    public string RoleName { get; }

    /// <summary>Короткое имя для сайдбара: «Даниил Г. А.» — помещается в 150px без лишнего переноса.</summary>
    public string UserShortName
    {
        get
        {
            var parts = (CurrentUser.FullName ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return RoleName;
            if (parts.Length == 1) return parts[0];
            // Фамилия Имя [Отчество] → Имя Ф[. О.]
            var first   = parts[1];
            var lastI   = parts[0][0] + ".";
            var midI    = parts.Length > 2 ? " " + parts[2][0] + "." : string.Empty;
            return $"{first} {lastI}{midI}";
        }
    }

    /// <summary>Инициалы для мини-аватара (резервный вариант когда нет фото).</summary>
    public string UserInitials
    {
        get
        {
            var parts = (CurrentUser.FullName ?? "?")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant()
                : parts.Length == 1 ? parts[0][..1].ToUpperInvariant() : "?";
        }
    }

    /// <summary>Фото для мини-аватара в сайдбаре (null → показывать инициалы).</summary>
    public ImageSource? SidebarAvatarSource
    {
        get => _sidebarAvatarSource;
        private set
        {
            if (SetProperty(ref _sidebarAvatarSource, value))
                OnPropertyChanged(nameof(HasSidebarAvatar));
        }
    }

    public bool HasSidebarAvatar => _sidebarAvatarSource is not null;

    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    // ── RBAC ─────────────────────────────────────────────────────────────

    public bool IsClientsVisible    => !IsMechanic;   // механики не видят раздел клиентов
    public bool IsCarsVisible       => !IsMechanic;   // механики не видят раздел авто
    public bool IsWarehouseVisible  => true;           // механики видят (read-only)
    public bool IsReferencesVisible => !IsMechanic;
    public bool IsAuditVisible      => !IsMechanic;
    public bool IsReportsVisible    => !IsMechanic;

    private bool IsMechanic =>
        string.Equals(RoleName, MechanicRoleName, StringComparison.OrdinalIgnoreCase);

    // ── Команды навигации ─────────────────────────────────────────────────

    public ICommand NavigateClientsCommand    { get; }
    public ICommand NavigateCarsCommand       { get; }
    public ICommand NavigateOrdersCommand     { get; }
    public ICommand NavigateWarehouseCommand  { get; }
    public ICommand NavigateMechanicsCommand  { get; }
    public ICommand NavigateReferencesCommand { get; }
    public ICommand NavigateAuditCommand      { get; }
    public ICommand NavigateReportsCommand    { get; }
    public ICommand NavigateProfileCommand    { get; }
    public ICommand LogoutCommand             { get; }

    public void NavigateToCarsForClient(int clientId)
        => CurrentView = new CarsView(clientId);

    public void NavigateToClientsWithSearch(string searchText)
        => CurrentView = new ClientsView(searchText);

    // ── Аватар сайдбара ───────────────────────────────────────────────────

    private void OnAvatarChanged(object? sender, int userId)
    {
        if (userId == CurrentUser.UserID)
            LoadSidebarAvatar();
    }

    private void LoadSidebarAvatar()
    {
        var path = UserAvatarStorage.GetAvatarPath(CurrentUser.UserID);
        if (path is not null && File.Exists(path))
        {
            try
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.UriSource   = new Uri(path, UriKind.Absolute);
                img.EndInit();
                img.Freeze();
                SidebarAvatarSource = img;
                return;
            }
            catch { }
        }
        SidebarAvatarSource = null;
    }

    // ── Выход ─────────────────────────────────────────────────────────────

    private static void ExecuteLogout()
    {
        CurrentUser.Logout();

        var login = new LoginWindow();
        login.Show();

        foreach (Window window in Application.Current.Windows)
        {
            if (window is MainWindow main)
            {
                main.Close();
                break;
            }
        }
    }
}
