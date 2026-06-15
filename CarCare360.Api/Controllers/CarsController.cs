using CarCare360.Api.Helpers;
using CarCare360.Api.Models.Dtos;
using CarCare360.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarCare360.Api.Controllers;

/// <summary>Управление автомобилями.</summary>
[ApiController]
[Route("api/cars")]
[Authorize]
[Produces("application/json")]
public class CarsController : ControllerBase
{
    private readonly ICarService _cars;

    public CarsController(ICarService cars) => _cars = cars;

    /// <summary>Список автомобилей: администратор — все, клиент — только свои.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CarDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CarDto>>> GetAll()
        => Ok(await _cars.GetAllAsync(CurrentUser.From(User)));

    /// <summary>Детали автомобиля.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CarDto>> GetById(int id)
        => Ok(await _cars.GetByIdAsync(id, CurrentUser.From(User)));

    /// <summary>Добавление автомобиля (привязка к текущему клиенту).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CarDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CarDto>> Create([FromBody] CarCreateRequest request)
    {
        var car = await _cars.CreateAsync(request, CurrentUser.From(User));
        return CreatedAtAction(nameof(GetById), new { id = car.CarID }, car);
    }

    /// <summary>Обновление автомобиля.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CarDto>> Update(int id, [FromBody] CarUpdateRequest request)
        => Ok(await _cars.UpdateAsync(id, request, CurrentUser.From(User)));

    /// <summary>Удаление автомобиля.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        await _cars.DeleteAsync(id, CurrentUser.From(User));
        return NoContent();
    }
}
