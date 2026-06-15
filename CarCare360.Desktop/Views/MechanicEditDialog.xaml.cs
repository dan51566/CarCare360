using CarCare360.Desktop.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace CarCare360.Desktop.Views;

/// <summary>Диалог добавления / редактирования механика.</summary>
public partial class MechanicEditDialog : Window
{
    public MechanicEditDialog()
    {
        InitializeComponent();
    }

    private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MechanicEditViewModel vm && sender is PasswordBox pb)
            vm.Password = pb.Password;
    }
}
