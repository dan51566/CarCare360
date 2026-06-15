namespace CarCare360.Api.Models.Dtos;

/// <summary>Запись журнала аудита для ответов API.</summary>
public class AuditLogDto
{
    public long LogID { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string? PrimaryKeyValue { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime? ChangedAt { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IPAddress { get; set; }
}

/// <summary>Постраничный результат (обёртка для списков).</summary>
/// <typeparam name="T">Тип элемента.</typeparam>
public class PagedResult<T>
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<T> Items { get; set; } = new();
}
