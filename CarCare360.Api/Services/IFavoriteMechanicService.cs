using CarCare360.Api.Models.Dtos;

namespace CarCare360.Api.Services;

/// <summary>
/// Сервис избранных механиков клиента (Изменение №2, Доработка 3).
/// Все операции работают строго в пределах данных текущего пользователя:
/// идентификатор клиента передаётся из контроллера (берётся из JWT), а не из запроса.
/// </summary>
public interface IFavoriteMechanicService
{
    /// <summary>
    /// Каталог активных механиков для клиента с признаком избранного.
    /// Избранные механики идут первыми; внутри групп — по алфавиту.
    /// Деактивированные механики (Users.IsActive = 0 / IsDeleted = 1) не включаются.
    /// </summary>
    Task<List<MechanicCatalogDto>> GetCatalogAsync(int userId);

    /// <summary>Добавить механика в избранное клиента (идемпотентно).</summary>
    Task AddAsync(int userId, int mechanicId);

    /// <summary>Убрать механика из избранного клиента (идемпотентно).</summary>
    Task RemoveAsync(int userId, int mechanicId);
}
