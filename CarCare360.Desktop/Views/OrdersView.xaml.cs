using CarCare360.Desktop.ViewModels;
using System.Windows.Controls;

namespace CarCare360.Desktop.Views;

/// <summary>
/// Code-behind раздела «Заказы».
/// </summary>
public partial class OrdersView : UserControl
{
    public OrdersView()
    {
        InitializeComponent();
        DataContext = new OrdersViewModel();
        Loaded += async (_, _) =>
        {
            if (DataContext is OrdersViewModel vm)
                await vm.LoadOrdersAsync();
        };
    }

    private void ClearSearch_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is OrdersViewModel vm)
            vm.SearchText = string.Empty;
    }
}
