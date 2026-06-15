using System.Windows;
using System.Windows.Controls;

namespace CarCare360.Desktop.Helpers;

/// <summary>
/// Вложенное (attached) свойство для двусторонней привязки пароля в <see cref="PasswordBox"/>.
/// Стандартный PasswordBox не позволяет биндить свойство Password по соображениям безопасности,
/// поэтому этот хелпер реализует синхронизацию текста с строковым свойством ViewModel.
///
/// Использование в XAML:
/// <code>
/// <PasswordBox helpers:PasswordBoxHelper.BoundPassword="{Binding Password, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
/// </code>
/// </summary>
public static class PasswordBoxHelper
{
    /// <summary>
    /// Вложенное свойство, хранящее текущий пароль (синхронизируется с PasswordBox).
    /// </summary>
    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BoundPassword",
            typeof(string),
            typeof(PasswordBoxHelper),
            new FrameworkPropertyMetadata(string.Empty, OnBoundPasswordChanged));

    /// <summary>
    /// Внутренний флаг — отслеживает, что данный PasswordBox уже подписан на событие PasswordChanged.
    /// Это предотвращает повторную подписку при многократных установках значения.
    /// </summary>
    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached(
            "IsUpdating",
            typeof(bool),
            typeof(PasswordBoxHelper),
            new PropertyMetadata(false));

    public static string GetBoundPassword(DependencyObject obj) =>
        (string)obj.GetValue(BoundPasswordProperty);

    public static void SetBoundPassword(DependencyObject obj, string value) =>
        obj.SetValue(BoundPasswordProperty, value);

    private static bool GetIsUpdating(DependencyObject obj) =>
        (bool)obj.GetValue(IsUpdatingProperty);

    private static void SetIsUpdating(DependencyObject obj, bool value) =>
        obj.SetValue(IsUpdatingProperty, value);

    /// <summary>
    /// Реакция на изменение свойства BoundPassword извне (со стороны ViewModel).
    /// При первой установке подписывает PasswordBox на событие PasswordChanged,
    /// после чего синхронизирует Password только если изменение пришло не от самого PasswordBox.
    /// </summary>
    private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox passwordBox)
            return;

        // Подписываемся только один раз — повторных подписок не делаем.
        passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;

        if (!GetIsUpdating(passwordBox))
        {
            passwordBox.Password = (string?)e.NewValue ?? string.Empty;
        }

        passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
    }

    /// <summary>
    /// Когда пользователь меняет пароль в PasswordBox — записываем актуальное значение
    /// в BoundPassword. Флаг IsUpdating предотвращает зацикливание.
    /// </summary>
    private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox passwordBox)
            return;

        SetIsUpdating(passwordBox, true);
        SetBoundPassword(passwordBox, passwordBox.Password);
        SetIsUpdating(passwordBox, false);
    }
}
