using CarCare360.Desktop.ViewModels;
using System.Windows.Controls;

namespace CarCare360.Desktop.Views;

/// <summary>
/// Представление раздела «Автомобили».
/// Принимает опциональный фильтр по клиенту — используется при переходе
/// из раздела «Клиенты» по нажатию на колонку «Авто».
/// </summary>
public partial class CarsView : UserControl
{
    public CarsView(int? clientIdFilter = null)
    {
        InitializeComponent();
        DataContext = new CarsViewModel(clientIdFilter);

        Loaded += async (_, _) =>
        {
            if (DataContext is CarsViewModel vm)
                await vm.LoadCarsAsync();
        };
    }

    private void ClearSearch_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is CarsViewModel vm)
            vm.SearchText = string.Empty;
    }
}
