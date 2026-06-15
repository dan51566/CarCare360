using CarCare360.Api.Helpers;
using CarCare360.Api.Models.Dtos;

namespace CarCare360.Api.Services;

/// <summary>Сервис управления заказами-нарядами.</summary>
public interface IOrderService
{
    /// <summary>
    /// Список заказов: админ — все; клиент — свои; механик — назначенные ему.
    /// </summary>
    Task<List<OrderDto>> GetAllAsync(CurrentUser current);

    /// <summary>Детали заказа (услуги, запчасти, статус) с проверкой доступа.</summary>
    Task<OrderDto> GetByIdAsync(int id, CurrentUser current);

    /// <summary>Создание заказа (хранимые процедуры CreateOrder + AddServiceToOrder).</summary>
    Task<OrderDto> CreateAsync(OrderCreateRequest request, CurrentUser current);

    /// <summary>Изменение статуса заказа (хранимая процедура UpdateOrderStatus). Админ и механик.</summary>
    Task<OrderDto> UpdateStatusAsync(int id, string newStatus, CurrentUser current);

    /// <summary>Обновление заказа: клиент — только примечания, админ — все поля.</summary>
    Task<OrderDto> UpdateAsync(int id, OrderUpdateRequest request, CurrentUser current);

    /// <summary>Мягкое удаление заказа (IsDeleted = 1).</summary>
    Task DeleteAsync(int id, CurrentUser current);
}
