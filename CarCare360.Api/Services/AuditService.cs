using CarCare360.Api.Data;
using CarCare360.Api.Helpers;
using CarCare360.Api.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CarCare360.Api.Services;

/// <summary>Реализация чтения журнала аудита.</summary>
public class AuditService : IAuditService
{
    private readonly CarCareDbContext _db;

    public AuditService(CarCareDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<PagedResult<AuditLogDto>> GetAsync(
        string? tableName, string? operation, DateTime? from, DateTime? to, int page, int pageSize)
    {
        if (page < 1) page = 1;
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(tableName))
            query = query.Where(a => a.TableName == tableName);

        if (!string.IsNullOrWhiteSpace(operation))
            query = query.Where(a => a.Operation == operation);

        if (from.HasValue)
            query = query.Where(a => a.ChangedAt >= from.Value);

        if (to.HasValue)
        {
            var toExclusive = to.Value.Date.AddDays(1);
            query = query.Where(a => a.ChangedAt < toExclusive);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.LogID)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<AuditLogDto>
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Items = items.Select(a => a.ToDto()).ToList()
        };
    }
}
