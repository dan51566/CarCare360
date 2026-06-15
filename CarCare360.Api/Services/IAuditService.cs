using CarCare360.Api.Models.Dtos;

namespace CarCare360.Api.Services;

/// <summary>Сервис чтения журнала аудита (только для администратора).</summary>
public interface IAuditService
{
    /// <summary>
    /// Постраничный журнал аудита с фильтрами по таблице, операции и периоду.
    /// </summary>
    Task<PagedResult<AuditLogDto>> GetAsync(
        string? tableName, string? operation, DateTime? from, DateTime? to, int page, int pageSize);
}
