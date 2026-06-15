using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CarCare360.Desktop.Helpers;

/// <summary>
/// ContentControl со встроенной анимацией появления при смене содержимого:
/// fade-in (Opacity 0→1) + slide-up (TranslateTransform.Y 15→0) за 300ms.
/// Используется вместо обычного ContentControl в MainWindow.
/// </summary>
public class AnimatedContentControl : ContentControl
{
    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        if (newContent is null) return;

        // Начинаем с прозрачности = 0 и смещения +15px вниз
        Opacity = 0;
        var tt  = new TranslateTransform(0, 15);
        RenderTransform = tt;

        var sb  = new Storyboard();

        // Fade in
        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
        Storyboard.SetTarget(fade, this);
        Storyboard.SetTargetProperty(fade, new PropertyPath(UIElement.OpacityProperty));

        // Slide up
        var slide = new DoubleAnimation(15, 0, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(slide, tt);
        Storyboard.SetTargetProperty(slide, new PropertyPath(TranslateTransform.YProperty));

        sb.Children.Add(fade);
        sb.Children.Add(slide);
        sb.Begin();
    }
}
