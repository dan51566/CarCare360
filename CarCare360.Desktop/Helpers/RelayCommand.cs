using System.Windows.Input;

namespace CarCare360.Desktop.Helpers;

/// <summary>
/// Универсальная реализация <see cref="ICommand"/> для привязки команд в XAML
/// к методам ViewModel. Поддерживает синхронные и асинхронные действия,
/// а также проверку доступности через <see cref="CanExecute"/>.
/// </summary>
public sealed class RelayCommand : ICommand
{
    // Не readonly — нужна возможность задать в конструкторе async-вариант
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>
    /// Событие, оповещающее WPF о необходимости пересчитать доступность команды.
    /// Перенаправляется на <see cref="CommandManager.RequerySuggested"/>.
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add    { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    /// <summary>
    /// Создаёт синхронную команду без проверки доступности (всегда доступна).
    /// </summary>
    public RelayCommand(Action execute)
        : this(execute, null)
    {
    }

    /// <summary>
    /// Создаёт синхронную команду с проверкой доступности.
    /// </summary>
    public RelayCommand(Action execute, Func<bool>? canExecute)
    {
        _execute    = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// Создаёт асинхронную команду (Func&lt;Task&gt;).
    /// Исполнение обёртывается в async void — UI не блокируется,
    /// а исключения обрабатываются внутри Task.
    /// </summary>
    /// <param name="executeAsync">Асинхронный метод.</param>
    /// <param name="canExecute">Предикат доступности команды.</param>
    public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
    {
        if (executeAsync is null) throw new ArgumentNullException(nameof(executeAsync));

        // Оборачиваем async Task в async void: WPF требует Action в Execute()
        _execute    = async () => await executeAsync();
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    /// <inheritdoc />
    public void Execute(object? parameter) => _execute();

    /// <summary>
    /// Принудительно просит WPF перепроверить доступность команды.
    /// </summary>
    public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
}

/// <summary>
/// Обобщённая команда с параметром (CommandParameter).
/// </summary>
public sealed class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public event EventHandler? CanExecuteChanged
    {
        add    { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute    = execute    ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public RelayCommand(Func<T?, Task> executeAsync, Func<T?, bool>? canExecute = null)
    {
        if (executeAsync is null) throw new ArgumentNullException(nameof(executeAsync));
        _execute    = async param => await executeAsync(param);
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        var p = parameter is T t ? t : default;
        return _canExecute?.Invoke(p) ?? true;
    }

    public void Execute(object? parameter)
    {
        var p = parameter is T t ? t : default;
        _execute(p);
    }

    public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
}
