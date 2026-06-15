using CarCare360.Desktop.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace CarCare360.Desktop.Views;

/// <summary>
/// Представление раздела «Клиенты».
/// Code-behind: DataContext, загрузка данных, двойной клик, очистка поиска.
/// </summary>
public partial class ClientsView : UserControl
{
    public ClientsView(string? initialSearch = null)
    {
        InitializeComponent();
        var vm = new ClientsViewModel();
        if (!string.IsNullOrEmpty(initialSearch))
            vm.SearchText = initialSearch;
        DataContext = vm;

        Loaded += async (_, _) =>
        {
            if (DataContext is ClientsViewModel vm2)
                await vm2.LoadClientsAsync();
        };
    }

    /// <summary>Двойной клик — редактировать клиента (не удалённого).</summary>
    private void ClientsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ClientsViewModel vm &&
            vm.SelectedClient is { IsDeleted: false } &&
            vm.EditCommand.CanExecute(null))
        {
            vm.EditCommand.Execute(null);
        }
    }

    /// <summary>Кнопка ✕ в строке поиска — очищает фильтр.</summary>
    private void ClearSearch_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ClientsViewModel vm)
            vm.SearchText = string.Empty;
    }
}
