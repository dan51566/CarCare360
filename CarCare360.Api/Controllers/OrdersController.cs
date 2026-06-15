using CarCare360.Api.Helpers;
using CarCare360.Api.Models.Dtos;
using CarCare360.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarCare360.Api.Controllers;

/// <summary>Управление заказами-нарядами.</summary>
[ApiController]
[Route("api/orders")]
[Authorize]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders) => _orders = orders;

    /// <summary>
    /// Список заказов: администратор видит все, клиент — свои, механик — назначенные.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<OrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<OrderDto>>> GetAll()
        => Ok(await _orders.GetAllAsync(CurrentUser.From(User)));

    /// <summary>Детали заказа (услуги, запчасти, статус).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(int id)
        => Ok(await _orders.GetByIdAsync(id, CurrentUser.From(User)));

    /// <summary>Создание заказа (клиент или администратор).</summary>
    /// <remarks>Использует хранимые процедуры CreateOrder и AddServiceToOrder.</remarks>
    [HttpPost]
    [Authorize(Roles = Roles.Admin + "," + Roles.Client)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OrderDto>> Create([FromBody] OrderCreateRequest request)
    {
        var order = await _orders.CreateAsync(request, CurrentUser.From(User));
        return CreatedAtAction(nameof(GetById), new { id = order.OrderID }, order);
    }

    /// <summary>Изменение статуса заказа (администратор и механик).</summary>
    /// <remarks>Использует хранимую процедуру UpdateOrderStatus.</remarks>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> UpdateStatus(int id, [FromBody] OrderStatusUpdateRequest request)
        => Ok(await _orders.UpdateStatusAsync(id, request.Status, CurrentUser.From(User)));

    /// <summary>Обновление заказа (клиент — примечание, администратор — все поля).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin + "," + Roles.Client)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> Update(int id, [FromBody] OrderUpdateRequest request)
        => Ok(await _orders.UpdateAsync(id, request, CurrentUser.From(User)));

    /// <summary>Мягкое удаление заказа.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin + "," + Roles.Client)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await _orders.DeleteAsync(id, CurrentUser.From(User));
        return NoContent();
    }
}
