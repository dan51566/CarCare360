using System.Windows;
using System.Windows.Controls;

namespace CarCare360.Desktop.Views;

/// <summary>
/// Главное окно приложения.
/// Содержит боковое меню, AnimatedContentControl для перехода между разделами
/// и оверлей-контейнер для Toast-уведомлений.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Добавляет тост в верхний-правый контейнер уведомлений.
    /// Вызывается из <see cref="Helpers.ToastHelper.Show"/>.
    /// </summary>
    public void AddToast(ToastNotification toast)
    {
        ToastContainer.Children.Insert(0, toast);
    }
}
