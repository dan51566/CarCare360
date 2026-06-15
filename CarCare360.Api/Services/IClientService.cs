using CarCare360.Api.Helpers;
using CarCare360.Api.Models.Dtos;

namespace CarCare360.Api.Services;

/// <summary>Сервис управления клиентами (пользователями с ролью «Клиент»).</summary>
public interface IClientService
{
    /// <summary>Список клиентов (только для администратора).</summary>
    Task<List<UserDto>> GetAllAsync();

    /// <summary>Профиль клиента (для самого клиента или администратора).</summary>
    Task<UserDto> GetByIdAsync(int id, CurrentUser current);

    /// <summary>Обновление профиля клиента.</summary>
    Task<UserDto> UpdateAsync(int id, ClientUpdateRequest request, CurrentUser current);

    /// <summary>Мягкое удаление клиента (IsDeleted = 1). Только администратор.</summary>
    Task DeleteAsync(int id);

    /// <summary>Автомобили клиента.</summary>
    Task<List<CarDto>> GetCarsAsync(int id, CurrentUser current);
}
