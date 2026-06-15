using CarCare360.Desktop.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CarCare360.Desktop.Views;

/// <summary>
/// Окно авторизации с анимацией фона в стиле «лавовой лампы».
/// Код-behind содержит:
///  — синхронизацию пароля из PasswordBox в ViewModel;
///  — запуск blob-анимаций при загрузке окна.
/// </summary>
public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Передаём актуальный пароль в ViewModel при каждом изменении PasswordBox.
    /// Управляем видимостью placeholder-текста.
    /// </summary>
    private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            vm.Password = PasswordInput.Password;

        // Показываем/скрываем placeholder пароля
        PasswordPlaceholder.Visibility = string.IsNullOrEmpty(PasswordInput.Password)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    /// <summary>
    /// Запускаем анимации всех blob-элементов после загрузки окна.
    /// </summary>
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // blob1: большой синий — медленно плывёт по диагонали
        AnimateBlob(BlobTrans1, BlobScale1,
            txFrom: -60, txTo: 80,
            tyFrom: -60, tyTo: 120,
            durSec: 11,
            sxMin: 0.85, sxMax: 1.20,
            syMin: 0.90, syMax: 1.15,
            offsetSec: 0);

        // blob2: оранжевый — быстрее, смещается вниз-влево
        AnimateBlob(BlobTrans2, BlobScale2,
            txFrom: 280, txTo: 140,
            tyFrom: 30,  tyTo: 200,
            durSec: 9,
            sxMin: 0.80, sxMax: 1.25,
            syMin: 0.85, syMax: 1.20,
            offsetSec: 1.5);

        // blob3: фиолетовый — средняя скорость, большой ход
        AnimateBlob(BlobTrans3, BlobScale3,
            txFrom: 160, txTo: 30,
            tyFrom: 300, tyTo: 120,
            durSec: 13,
            sxMin: 0.75, sxMax: 1.30,
            syMin: 0.80, syMax: 1.25,
            offsetSec: 3.0);

        // blob4: тёмно-синий — снизу-слева вверх
        AnimateBlob(BlobTrans4, BlobScale4,
            txFrom: -20, txTo: 120,
            tyFrom: 340, tyTo: 180,
            durSec: 10,
            sxMin: 0.90, sxMax: 1.15,
            syMin: 0.85, syMax: 1.20,
            offsetSec: 0.8);

        // blob5: янтарный — компактный, вращается по малому кругу
        AnimateBlob(BlobTrans5, BlobScale5,
            txFrom: 310, txTo: 220,
            tyFrom: 260, tyTo: 360,
            durSec: 8,
            sxMin: 0.80, sxMax: 1.30,
            syMin: 0.75, syMax: 1.35,
            offsetSec: 2.2);

        // blob6: индиго — сверху, медленный
        AnimateBlob(BlobTrans6, BlobScale6,
            txFrom: 100, txTo: 250,
            tyFrom: -30, tyTo: 100,
            durSec: 12,
            sxMin: 0.85, sxMax: 1.18,
            syMin: 0.88, syMax: 1.15,
            offsetSec: 4.0);
    }

    /// <summary>
    /// Запускает непрерывную анимацию перемещения и пульсации одного blob.
    /// </summary>
    /// <param name="trans">TranslateTransform blob-элемента.</param>
    /// <param name="scale">ScaleTransform blob-элемента.</param>
    /// <param name="txFrom">Начальная позиция X.</param>
    /// <param name="txTo">Конечная позиция X.</param>
    /// <param name="tyFrom">Начальная позиция Y.</param>
    /// <param name="tyTo">Конечная позиция Y.</param>
    /// <param name="durSec">Длительность одной фазы анимации в секундах.</param>
    /// <param name="sxMin">Минимальный масштаб X (пульсация).</param>
    /// <param name="sxMax">Максимальный масштаб X (пульсация).</param>
    /// <param name="syMin">Минимальный масштаб Y (пульсация).</param>
    /// <param name="syMax">Максимальный масштаб Y (пульсация).</param>
    /// <param name="offsetSec">Сдвиг начала анимации (чтобы blob-ы не двигались синхронно).</param>
    private static void AnimateBlob(
        TranslateTransform trans, ScaleTransform scale,
        double txFrom, double txTo,
        double tyFrom, double tyTo,
        double durSec,
        double sxMin, double sxMax,
        double syMin, double syMax,
        double offsetSec)
    {
        var ease = new SineEase { EasingMode = EasingMode.EaseInOut };
        var duration = TimeSpan.FromSeconds(durSec);
        var beginTime = TimeSpan.FromSeconds(offsetSec);

        // ── Перемещение X ──────────────────────────────────────────────────
        trans.BeginAnimation(TranslateTransform.XProperty,
            new DoubleAnimation
            {
                From = txFrom,
                To = txTo,
                Duration = duration,
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = ease,
                BeginTime = beginTime
            });

        // ── Перемещение Y ──────────────────────────────────────────────────
        trans.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation
            {
                From = tyFrom,
                To = tyTo,
                Duration = TimeSpan.FromSeconds(durSec * 1.3), // разный ритм по Y
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = ease,
                BeginTime = beginTime
            });

        // ── Пульсация ScaleX ───────────────────────────────────────────────
        scale.BeginAnimation(ScaleTransform.ScaleXProperty,
            new DoubleAnimation
            {
                From = sxMin,
                To = sxMax,
                Duration = TimeSpan.FromSeconds(durSec * 0.7),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = ease,
                BeginTime = beginTime
            });

        // ── Пульсация ScaleY ───────────────────────────────────────────────
        scale.BeginAnimation(ScaleTransform.ScaleYProperty,
            new DoubleAnimation
            {
                From = syMin,
                To = syMax,
                Duration = TimeSpan.FromSeconds(durSec * 0.9),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = ease,
                BeginTime = beginTime
            });
    }
}
