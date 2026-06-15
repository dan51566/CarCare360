using CarCare360.Desktop.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace CarCare360.Desktop.Views;

/// <summary>
/// Всплывающее уведомление (Toast).
/// Появляется с анимацией выезда справа, через 3 секунды плавно исчезает.
/// Управляется через <see cref="ToastHelper.Show"/>.
/// </summary>
public partial class ToastNotification : UserControl
{
    public ToastNotification()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    /// <summary>Настраивает содержимое и цветовую схему тоста.</summary>
    public void Configure(string message, ToastType type)
    {
        MessageText.Text = message;
        var (bg, icon) = type switch
        {
            ToastType.Error   => (Color.FromRgb(211, 47,  47),  "✗"),
            ToastType.Warning => (Color.FromRgb(230, 81,   0),  "⚠"),
            ToastType.Info    => (Color.FromRgb( 21, 101, 192), "ℹ"),
            _                 => (Color.FromRgb( 46, 125,  50), "✔"),
        };
        ToastBorder.Background = new SolidColorBrush(bg);
        IconText.Text = icon;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (_, _) => { timer.Stop(); FadeOut(); };
        timer.Start();
    }

    private void FadeOut()
    {
        var anim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(350));
        anim.Completed += (_, _) => (Parent as Panel)?.Children.Remove(this);
        BeginAnimation(OpacityProperty, anim);
    }
}
