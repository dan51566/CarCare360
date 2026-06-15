using CarCare360.Desktop.ViewModels;
using System.Windows.Controls;

namespace CarCare360.Desktop.Views;

public partial class ServicesPublicView : UserControl
{
    public ServicesPublicView()
    {
        InitializeComponent();
        DataContext = new ServicesPublicViewModel();
    }
}
