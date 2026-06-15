using CarCare360.Desktop.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace CarCare360.Desktop.Views;

public partial class ProfileView : UserControl
{
    public ProfileView()
    {
        InitializeComponent();
        DataContext = new ProfileViewModel();
    }

    private ProfileViewModel? Vm => DataContext as ProfileViewModel;

    private void PwdLoginCurrent_Changed(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.LoginPassword = PwdLoginCurrent.Password;
    }

    private void PwdOld_Changed(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.OldPassword = PwdOld.Password;
    }

    private void PwdNew_Changed(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.NewPassword = PwdNew.Password;
    }

    private void PwdConfirm_Changed(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.ConfirmPassword = PwdConfirm.Password;
    }
}
