using CarCare360.Desktop.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace CarCare360.Desktop.Views;

/// <summary>
/// Диалог добавления / редактирования клиента.
/// Code-behind: синхронизация PasswordBox → ViewModel.
/// Закрытие инициируется через событие <see cref="ClientEditViewModel.CloseRequested"/>
/// (подписка выполняется в <see cref="ClientsViewModel"/>).
/// </summary>
public partial class ClientEditDialog : Window
{
    public ClientEditDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Передаём актуальный пароль в ViewModel при каждом нажатии клавиши.
    /// Стандартная практика WPF — PasswordBox не поддерживает data binding напрямую.
    /// </summary>
    private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ClientEditViewModel vm)
            vm.Password = PasswordInput.Password;
    }
}
