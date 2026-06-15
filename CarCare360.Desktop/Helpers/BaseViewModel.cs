using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CarCare360.Desktop.Helpers;

/// <summary>
/// Базовый класс для всех ViewModel приложения.
/// Реализует <see cref="INotifyPropertyChanged"/> и предоставляет
/// удобный метод <see cref="SetProperty{T}"/> для обновления полей с уведомлением.
/// </summary>
public abstract class BaseViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// Событие, сигнализирующее об изменении свойства.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Вызывает событие <see cref="PropertyChanged"/> для указанного свойства.
    /// Имя свойства подставляется автоматически благодаря <see cref="CallerMemberNameAttribute"/>.
    /// </summary>
    /// <param name="propertyName">Имя изменившегося свойства.</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Устанавливает значение поля и, если оно изменилось, уведомляет об этом подписчиков.
    /// </summary>
    /// <typeparam name="T">Тип значения свойства.</typeparam>
    /// <param name="field">Ссылка на приватное поле, хранящее значение.</param>
    /// <param name="value">Новое значение.</param>
    /// <param name="propertyName">Имя свойства (подставляется автоматически).</param>
    /// <returns>true, если значение было изменено; false, если осталось прежним.</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
