using CarCare360.Api.Helpers;
using CarCare360.Api.Models.Dtos;
using CarCare360.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarCare360.Api.Controllers;

/// <summary>
/// Избранные механики клиента (Изменение №2, Доработка 3).
/// Доступно только роли «Клиент»; все операции — строго в пределах данных
/// текущего пользователя (userId берётся из JWT, не из запроса).
/// </summary>
[ApiController]
[Route("api/favorite-mechanics")]
[Authorize(Roles = Roles.Client)]
[Produces("application/json")]
public class FavoriteMechanicsController : ControllerBase
{
    private readonly IFavoriteMechanicService _favorites;

    public FavoriteMechanicsController(IFavoriteMechanicService favorites) => _favorites = favorites;

    /// <summary>Каталог активных механиков с признаком избранного (избранные — первыми).</summary>
    [HttpGet("catalog")]
    [ProducesResponseType(typeof(List<MechanicCatalogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MechanicCatalogDto>>> GetCatalog()
        => Ok(await _favorites.GetCatalogAsync(User.GetUserId()));

    /// <summary>Добавить механика в избранное текущего клиента.</summary>
    [HttpPost("{mechanicId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(int mechanicId)
    {
        await _favorites.AddAsync(User.GetUserId(), mechanicId);
        return NoContent();
    }

    /// <summary>Убрать механика из избранного текущего клиента.</summary>
    [HttpDelete("{mechanicId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remove(int mechanicId)
    {
        await _favorites.RemoveAsync(User.GetUserId(), mechanicId);
        return NoContent();
    }
}
