using CarCare360.Desktop.ViewModels;
using System.Windows.Controls;

namespace CarCare360.Desktop.Views;

public partial class ClientCarsView : UserControl
{
    public ClientCarsView()
    {
        InitializeComponent();
        DataContext = new ClientCarsViewModel();
    }
}
