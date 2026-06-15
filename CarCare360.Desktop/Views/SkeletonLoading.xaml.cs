using System.Windows.Controls;

namespace CarCare360.Desktop.Views;

/// <summary>
/// Заглушка загрузки (Skeleton Screen) — показывается вместо DataGrid,
/// пока выполняется загрузка данных. Прямоугольники пульсируют.
/// </summary>
public partial class SkeletonLoading : UserControl
{
    public SkeletonLoading()
    {
        InitializeComponent();
    }
}
