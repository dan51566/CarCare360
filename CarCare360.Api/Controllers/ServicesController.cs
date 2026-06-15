using CarCare360.Api.Helpers;
using CarCare360.Api.Models.Dtos;
using CarCare360.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarCare360.Api.Controllers;

/// <summary>Справочник услуг автосервиса.</summary>
[ApiController]
[Route("api/services")]
[Produces("application/json")]
public class ServicesController : ControllerBase
{
    private readonly ICatalogService _catalog;

    public ServicesController(ICatalogService catalog) => _catalog = catalog;

    /// <summary>Список услуг (публично — для записи на сервис).</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ServiceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ServiceDto>>> GetAll()
        => Ok(await _catalog.GetServicesAsync());

    /// <summary>Детали услуги.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceDto>> GetById(int id)
        => Ok(await _catalog.GetServiceAsync(id));

    /// <summary>Создание услуги (администратор).</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ServiceDto>> Create([FromBody] ServiceUpsertRequest request)
    {
        var service = await _catalog.CreateServiceAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = service.ServiceID }, service);
    }

    /// <summary>Обновление услуги (администратор).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceDto>> Update(int id, [FromBody] ServiceUpsertRequest request)
        => Ok(await _catalog.UpdateServiceAsync(id, request));

    /// <summary>Удаление услуги (администратор).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        await _catalog.DeleteServiceAsync(id);
        return NoContent();
    }
}
