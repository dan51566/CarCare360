using CarCare360.Api.Helpers;
using CarCare360.Api.Models.Dtos;
using CarCare360.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarCare360.Api.Controllers;

/// <summary>Аналитические отчёты (только администратор).</summary>
[ApiController]
[Route("api/reports")]
[Authorize(Roles = Roles.Admin)]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;

    /// <summary>Загрузка механиков за период.</summary>
    /// <param name="from">Начало периода (включительно).</param>
    /// <param name="to">Конец периода (включительно).</param>
    [HttpGet("mechanics-load")]
    [ProducesResponseType(typeof(List<MechanicsLoadReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<MechanicsLoadReportDto>>> MechanicsLoad(
        [FromQuery] DateTime from, [FromQuery] DateTime to)
        => Ok(await _reports.GetMechanicsLoadAsync(from, to));

    /// <summary>Финансовый отчёт за период.</summary>
    /// <param name="from">Начало периода (включительно).</param>
    /// <param name="to">Конец периода (включительно).</param>
    [HttpGet("financial")]
    [ProducesResponseType(typeof(FinancialReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FinancialReportDto>> Financial(
        [FromQuery] DateTime from, [FromQuery] DateTime to)
        => Ok(await _reports.GetFinancialAsync(from, to));
}
