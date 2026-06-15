using CarCare360.Api.Helpers;
using CarCare360.Api.Models.Dtos;
using CarCare360.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarCare360.Api.Controllers;

/// <summary>Управление клиентами и их автомобилями.</summary>
[ApiController]
[Route("api/clients")]
[Authorize]
[Produces("application/json")]
public class ClientsController : ControllerBase
{
    private readonly IClientService _clients;

    public ClientsController(IClientService clients) => _clients = clients;

    /// <summary>Список клиентов (только администратор).</summary>
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserDto>>> GetAll()
        => Ok(await _clients.GetAllAsync());

    /// <summary>Профиль клиента (сам клиент или администратор).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(int id)
        => Ok(await _clients.GetByIdAsync(id, CurrentUser.From(User)));

    /// <summary>Обновление профиля клиента.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> Update(int id, [FromBody] ClientUpdateRequest request)
        => Ok(await _clients.UpdateAsync(id, request, CurrentUser.From(User)));

    /// <summary>Мягкое удаление клиента (только администратор).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await _clients.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>Автомобили клиента (сам клиент или администратор).</summary>
    [HttpGet("{id:int}/cars")]
    [ProducesResponseType(typeof(List<CarDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<CarDto>>> GetCars(int id)
        => Ok(await _clients.GetCarsAsync(id, CurrentUser.From(User)));
}
