using CarCare360.Desktop.ViewModels;
using System.Windows.Controls;

namespace CarCare360.Desktop.Views;

public partial class ClientOrdersView : UserControl
{
    public ClientOrdersView()
    {
        InitializeComponent();
        DataContext = new ClientOrdersViewModel();
    }
}
