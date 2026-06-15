using CarCare360.Api.Helpers;
using CarCare360.Api.Models.Dtos;
using CarCare360.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarCare360.Api.Controllers;

/// <summary>Склад запчастей.</summary>
[ApiController]
[Route("api/parts")]
[Authorize]
[Produces("application/json")]
public class PartsController : ControllerBase
{
    private readonly ICatalogService _catalog;

    public PartsController(ICatalogService catalog) => _catalog = catalog;

    /// <summary>Список запчастей (только для сотрудников — администратор и механик).</summary>
    [HttpGet]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(typeof(List<PartDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PartDto>>> GetAll()
        => Ok(await _catalog.GetPartsAsync());

    /// <summary>Детали запчасти (сотрудники).</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(typeof(PartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PartDto>> GetById(int id)
        => Ok(await _catalog.GetPartAsync(id));

    /// <summary>Добавление запчасти (администратор).</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(PartDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PartDto>> Create([FromBody] PartUpsertRequest request)
    {
        var part = await _catalog.CreatePartAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = part.PartID }, part);
    }

    /// <summary>Обновление запчасти (администратор).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(PartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PartDto>> Update(int id, [FromBody] PartUpsertRequest request)
        => Ok(await _catalog.UpdatePartAsync(id, request));

    /// <summary>Удаление запчасти (администратор).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        await _catalog.DeletePartAsync(id);
        return NoContent();
    }
}
