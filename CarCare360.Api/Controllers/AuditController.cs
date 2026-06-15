using CarCare360.Api.Helpers;
using CarCare360.Api.Models.Dtos;
using CarCare360.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarCare360.Api.Controllers;

/// <summary>Журнал аудита (только администратор).</summary>
[ApiController]
[Route("api/audit")]
[Authorize(Roles = Roles.Admin)]
[Produces("application/json")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _audit;

    public AuditController(IAuditService audit) => _audit = audit;

    /// <summary>Журнал аудита с фильтрацией и постраничной выдачей.</summary>
    /// <param name="tableName">Фильтр по имени таблицы (Users, Cars, Orders).</param>
    /// <param name="operation">Фильтр по операции: I, U или D.</param>
    /// <param name="from">Начало периода (по ChangedAt).</param>
    /// <param name="to">Конец периода (по ChangedAt).</param>
    /// <param name="page">Номер страницы (с 1).</param>
    /// <param name="pageSize">Размер страницы (1–200).</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> Get(
        [FromQuery] string? tableName,
        [FromQuery] string? operation,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
        => Ok(await _audit.GetAsync(tableName, operation, from, to, page, pageSize));
}
