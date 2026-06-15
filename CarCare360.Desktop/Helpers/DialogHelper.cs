using System.Windows;

namespace CarCare360.Desktop.Helpers;

/// <summary>
/// Вспомогательные методы для открытия диалоговых окон.
/// </summary>
public static class DialogHelper
{
    /// <summary>
    /// Устанавливает Owner диалога безопасно: не ставит окно владельцем самого себя
    /// и игнорирует случаи, когда MainWindow ещё не показан или уже закрыт.
    /// </summary>
    public static void SetOwner(Window dialog)
    {
        try
        {
            var owner = Application.Current.MainWindow;
            if (owner != null && owner != dialog && owner.IsLoaded)
                dialog.Owner = owner;
        }
        catch
        {
            // игнорируем — диалог откроется без Owner
        }
    }
}
