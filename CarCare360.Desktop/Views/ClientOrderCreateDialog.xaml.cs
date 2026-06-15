using CarCare360.Desktop.ViewModels;
using System.Windows;

namespace CarCare360.Desktop.Views;

public partial class ClientOrderCreateDialog : Window
{
    public ClientOrderCreateDialog()
    {
        InitializeComponent();
        var vm = new ClientOrderCreateViewModel();
        vm.RequestClose += (_, _) =>
        {
            DialogResult = vm.DialogResult;
            Close();
        };
        DataContext = vm;
    }
}
