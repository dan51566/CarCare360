namespace CarCare360.Api.Helpers;

/// <summary>
/// Допустимые статусы заказа. Совпадают с CHECK-ограничением в БД и
/// проверкой в хранимой процедуре UpdateOrderStatus.
/// </summary>
public static class OrderStatuses
{
    public const string New = "Новый";
    public const string Assigned = "Назначен";
    public const string InProgress = "В работе";
    public const string WaitingParts = "Ожидает запчасти";
    public const string Ready = "Готов";
    public const string Issued = "Выдан";
    public const string Cancelled = "Отменён";

    /// <summary>Полный набор допустимых статусов.</summary>
    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        New, Assigned, InProgress, WaitingParts, Ready, Issued, Cancelled
    };

    /// <summary>Проверяет, что статус допустим.</summary>
    public static bool IsValid(string status) => All.Contains(status);
}
