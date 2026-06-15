using System.Windows;
using System.Windows.Controls;

namespace CarCare360.Desktop.Views;

public partial class ClientWindow : Window
{
    public ClientWindow()
    {
        InitializeComponent();
    }

    public void AddToast(ToastNotification toast)
    {
        ToastContainer.Children.Insert(0, toast);
    }
}
