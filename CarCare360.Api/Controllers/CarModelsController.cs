using CarCare360.Api.Models.Dtos;
using CarCare360.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarCare360.Api.Controllers;

/// <summary>
/// Справочник марок и моделей автомобилей.
/// Только чтение; используется мобильным приложением при добавлении автомобиля,
/// чтобы клиент выбирал готовый ModelID вместо ручного ввода марки/модели.
/// </summary>
[ApiController]
[Route("api/car-models")]
[Produces("application/json")]
public class CarModelsController : ControllerBase
{
    private readonly ICatalogService _catalog;

    public CarModelsController(ICatalogService catalog) => _catalog = catalog;

    /// <summary>Список моделей с марками (публично — для формы добавления авто).</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<CarModelDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CarModelDto>>> GetAll()
        => Ok(await _catalog.GetCarModelsAsync());
}
