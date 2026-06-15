using CarCare360.Desktop.ViewModels;
using System.Windows;

namespace CarCare360.Desktop.Views;

public partial class ClientCarAddDialog : Window
{
    public ClientCarAddDialog()
    {
        InitializeComponent();
        var vm = new ClientCarAddViewModel();
        vm.RequestClose += (_, _) =>
        {
            DialogResult = vm.DialogResult;
            Close();
        };
        DataContext = vm;
    }
}
