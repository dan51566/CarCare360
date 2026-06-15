using CarCare360.Desktop.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace CarCare360.Desktop.Views;

/// <summary>
/// Раздел «Журнал аудита» — только чтение, фильтры, детальная панель.
/// </summary>
public partial class AuditView : UserControl
{
    public AuditView()
    {
        InitializeComponent();
        DataContext = new AuditViewModel();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AuditViewModel vm)
            await vm.LoadAuditAsync();
    }
}
