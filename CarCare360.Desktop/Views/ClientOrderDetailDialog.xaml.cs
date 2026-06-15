using CarCare360.Desktop.ViewModels;
using System.Windows;

namespace CarCare360.Desktop.Views;

public partial class ClientOrderDetailDialog : Window
{
    public ClientOrderDetailDialog(int orderId)
    {
        InitializeComponent();
        var vm = new ClientOrderDetailViewModel(orderId);
        vm.RequestClose += (_, _) => Close();
        DataContext = vm;
    }
}
