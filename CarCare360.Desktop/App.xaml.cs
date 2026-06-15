using CarCare360.Desktop.Data;
using CarCare360.Desktop.Helpers;
using CarCare360.Desktop.Views;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace CarCare360.Desktop;

/// <summary>
/// Точка входа WPF-приложения CarCare 360.
/// </summary>
public partial class App : Application
{
    /// <summary>Путь к файлу лога рядом с exe.</summary>
    private static readonly string LogPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_error.log");

    private async void App_OnStartup(object sender, StartupEventArgs e)
    {
        // Глобальный перехватчик необработанных исключений UI-потока
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        try
        {
            await using var db = new CarCareDbContext();
            await DatabaseSeeder.SeedAsync(db);
        }
        catch (Exception ex)
        {
            // Пишем полный стек в лог-файл
            WriteLog("Seeder error", ex);

            MessageBox.Show(
                $"Не удалось инициализировать базу данных:\n\n" +
                $"{ex.GetType().Name}: {ex.Message}" +
                (ex.InnerException is not null ? $"\n\nInner: {ex.InnerException.Message}" : "") +
                $"\n\nПодробности — в файле:\n{LogPath}",
                "Предупреждение",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        new LoginWindow().Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteLog("Unhandled dispatcher exception", e.Exception);
        MessageBox.Show(
            $"Необработанная ошибка:\n{e.Exception.Message}\n\nСм. {LogPath}",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void WriteLog(string context, Exception ex)
    {
        try
        {
            File.WriteAllText(LogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}\n" +
                $"Type: {ex.GetType().FullName}\n" +
                $"Message: {ex.Message}\n" +
                (ex.InnerException is not null
                    ? $"Inner ({ex.InnerException.GetType().Name}): {ex.InnerException.Message}\n"
                    : "") +
                $"StackTrace:\n{ex.StackTrace}\n");
        }
        catch { /* не можем логировать — игнорируем */ }
    }
}
