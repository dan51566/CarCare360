namespace CarCare360.Api.Models.Dtos;

/// <summary>
/// Механик в каталоге выбора клиента (мобильное приложение) с признаком избранного.
/// Изменение №2, Доработка 3. Используется только клиентским эндпоинтом — в отличие
/// от админского <see cref="MechanicDto"/> не раскрывает логин/email/телефон механика.
/// </summary>
public class MechanicCatalogDto
{
    /// <summary>Идентификатор механика (Mechanics.MechanicID).</summary>
    public int MechanicID { get; set; }

    /// <summary>ФИО механика.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Специализация (может отсутствовать).</summary>
    public string? SpecializationName { get; set; }

    /// <summary>Уровень квалификации (может отсутствовать).</summary>
    public string? QualificationLevel { get; set; }

    /// <summary>Отмечен ли механик как избранный текущим клиентом.</summary>
    public bool IsFavorite { get; set; }
}
