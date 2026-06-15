using CarCare360.Api.Helpers;
using CarCare360.Api.Models.Dtos;
using CarCare360.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarCare360.Api.Controllers;

/// <summary>Управление механиками (только администратор).</summary>
[ApiController]
[Route("api/mechanics")]
[Authorize(Roles = Roles.Admin)]
[Produces("application/json")]
public class MechanicsController : ControllerBase
{
    private readonly IMechanicService _mechanics;

    public MechanicsController(IMechanicService mechanics) => _mechanics = mechanics;

    /// <summary>Список механиков.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MechanicDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MechanicDto>>> GetAll()
        => Ok(await _mechanics.GetAllAsync());

    /// <summary>Создание механика (создаёт пользователя с ролью «Механик» и профиль).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MechanicDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MechanicDto>> Create([FromBody] MechanicCreateRequest request)
    {
        var mechanic = await _mechanics.CreateAsync(request);
        return CreatedAtAction(nameof(GetAll), new { id = mechanic.MechanicID }, mechanic);
    }

    /// <summary>Загрузка механика — назначенные ему услуги (заказы).</summary>
    [HttpGet("{id:int}/schedule")]
    [ProducesResponseType(typeof(List<MechanicScheduleItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<MechanicScheduleItemDto>>> GetSchedule(int id)
        => Ok(await _mechanics.GetScheduleAsync(id));
}
