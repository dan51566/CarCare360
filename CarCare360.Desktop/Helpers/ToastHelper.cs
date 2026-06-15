using CarCare360.Desktop.Views;
using System.Windows;

namespace CarCare360.Desktop.Helpers;

/// <summary>Тип уведомления для ToastNotification.</summary>
public enum ToastType { Success, Error, Warning, Info }

/// <summary>
/// Статический помощник для показа всплывающих уведомлений (Toast).
/// Добавляет тост в оверлей-контейнер главного окна.
/// </summary>
public static class ToastHelper
{
    /// <summary>
    /// Показывает тост в правом верхнем углу MainWindow.
    /// Безопасен для вызова из не-UI потоков.
    /// </summary>
    public static void Show(string message, ToastType type = ToastType.Success)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            var toast = new ToastNotification();
            toast.Configure(message, type);

            if (Application.Current.MainWindow is MainWindow mw)
                mw.AddToast(toast);
            else if (Application.Current.MainWindow is ClientWindow cw)
                cw.AddToast(toast);
        });
    }
}
