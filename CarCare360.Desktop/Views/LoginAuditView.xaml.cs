using CarCare360.Desktop.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace CarCare360.Desktop.Views;

/// <summary>
/// Раздел «Аудит входов» (Изменение №2, Доработка 4) — только чтение, фильтры,
/// выделение подозрительной активности. Доступен только администратору.
/// </summary>
public partial class LoginAuditView : UserControl
{
    public LoginAuditView()
    {
        InitializeComponent();
        DataContext = new LoginAuditViewModel();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginAuditViewModel vm)
            await vm.LoadAsync();
    }
}
