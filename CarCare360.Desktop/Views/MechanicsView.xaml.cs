using CarCare360.Desktop.ViewModels;
using System.Windows.Controls;

namespace CarCare360.Desktop.Views;

/// <summary>Code-behind раздела «Механики».</summary>
public partial class MechanicsView : UserControl
{
    public MechanicsView()
    {
        InitializeComponent();
        DataContext = new MechanicsViewModel();
        Loaded += async (_, _) =>
        {
            if (DataContext is MechanicsViewModel vm)
                await vm.LoadMechanicsAsync();
        };
    }

    private void ClearSearch_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is MechanicsViewModel vm)
            vm.SearchText = string.Empty;
    }
}
