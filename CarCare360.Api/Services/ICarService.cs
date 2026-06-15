using CarCare360.Api.Helpers;
using CarCare360.Api.Models.Dtos;

namespace CarCare360.Api.Services;

/// <summary>Сервис управления автомобилями.</summary>
public interface ICarService
{
    /// <summary>Список автомобилей: админ видит все, клиент — только свои.</summary>
    Task<List<CarDto>> GetAllAsync(CurrentUser current);

    /// <summary>Детали автомобиля (с проверкой владельца).</summary>
    Task<CarDto> GetByIdAsync(int id, CurrentUser current);

    /// <summary>Добавление автомобиля (привязка к текущему клиенту либо к указанному — для админа).</summary>
    Task<CarDto> CreateAsync(CarCreateRequest request, CurrentUser current);

    /// <summary>Обновление автомобиля (с проверкой владельца).</summary>
    Task<CarDto> UpdateAsync(int id, CarUpdateRequest request, CurrentUser current);

    /// <summary>Удаление автомобиля (с проверкой владельца).</summary>
    Task DeleteAsync(int id, CurrentUser current);
}
