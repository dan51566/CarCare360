using CarCare360.Desktop.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace CarCare360.Desktop.Views;

/// <summary>
/// Раздел «Отчёты» — KPI-дашборд за выбранный период.
/// </summary>
public partial class ReportsView : UserControl
{
    public ReportsView()
    {
        InitializeComponent();
        DataContext = new ReportsViewModel();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ReportsViewModel vm)
            await vm.LoadAsync();
    }
}
