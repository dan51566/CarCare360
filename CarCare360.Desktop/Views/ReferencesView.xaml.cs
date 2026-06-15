using CarCare360.Desktop.ViewModels;
using System.Windows.Controls;

namespace CarCare360.Desktop.Views;

/// <summary>
/// Раздел «Справочники» — TabControl с 4 вкладками (Услуги, Боксы, Специализации, Марки и модели).
/// </summary>
public partial class ReferencesView : UserControl
{
    public ReferencesView()
    {
        InitializeComponent();
        DataContext = new ReferencesViewModel();
    }
}
