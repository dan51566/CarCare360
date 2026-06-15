using CarCare360.Api.Models.Dtos;

namespace CarCare360.Api.Services;

/// <summary>Сервис управления механиками (только для администратора).</summary>
public interface IMechanicService
{
    /// <summary>Список механиков.</summary>
    Task<List<MechanicDto>> GetAllAsync();

    /// <summary>Создание механика: создаёт пользователя с ролью «Механик» и профиль.</summary>
    Task<MechanicDto> CreateAsync(MechanicCreateRequest request);

    /// <summary>Загрузка механика — назначенные ему услуги (заказы).</summary>
    Task<List<MechanicScheduleItemDto>> GetScheduleAsync(int mechanicId);
}
