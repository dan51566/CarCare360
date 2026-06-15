using CarCare360.Desktop.ViewModels;
using System.Windows.Controls;

namespace CarCare360.Desktop.Views;

/// <summary>
/// Code-behind раздела «Склад».
/// </summary>
public partial class WarehouseView : UserControl
{
    public WarehouseView()
    {
        InitializeComponent();
        DataContext = new WarehouseViewModel();
        Loaded += async (_, _) =>
        {
            if (DataContext is WarehouseViewModel vm)
                await vm.LoadPartsAsync();
        };
    }

    private void ClearSearch_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is WarehouseViewModel vm)
            vm.SearchText = string.Empty;
    }
}
