using CarCare360.Desktop.ViewModels;
using System.Windows;

namespace CarCare360.Desktop.Views;

public partial class RegistrationWindow : Window
{
    public RegistrationWindow()
    {
        InitializeComponent();
    }

    private void PwdBox_Changed(object sender, RoutedEventArgs e)
    {
        if (DataContext is RegistrationViewModel vm)
        {
            vm.Password = PwdBox.Password;
            PwdPlaceholder.Visibility = string.IsNullOrEmpty(PwdBox.Password)
                ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void ConfirmPwdBox_Changed(object sender, RoutedEventArgs e)
    {
        if (DataContext is RegistrationViewModel vm)
        {
            vm.ConfirmPassword = ConfirmPwdBox.Password;
            ConfirmPwdPlaceholder.Visibility = string.IsNullOrEmpty(ConfirmPwdBox.Password)
                ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void ClientRole_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is RegistrationViewModel vm) vm.IsMechanic = false;
    }

    private void MechanicRole_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is RegistrationViewModel vm) vm.IsMechanic = true;
    }
}
