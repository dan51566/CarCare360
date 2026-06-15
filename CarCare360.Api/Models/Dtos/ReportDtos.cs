namespace CarCare360.Api.Models.Dtos;

/// <summary>Строка отчёта о загрузке механиков за период.</summary>
public class MechanicsLoadReportDto
{
    public int MechanicID { get; set; }
    public string MechanicName { get; set; } = string.Empty;
    public string? SpecializationName { get; set; }

    /// <summary>Всего назначенных услуг за период.</summary>
    public int TotalServices { get; set; }

    /// <summary>Из них выполнено.</summary>
    public int CompletedServices { get; set; }

    /// <summary>Суммарные нормо-часы по назначенным услугам.</summary>
    public decimal TotalNormHours { get; set; }
}

/// <summary>Финансовый отчёт за период.</summary>
public class FinancialReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    /// <summary>Количество заказов за период.</summary>
    public int OrdersCount { get; set; }

    /// <summary>Выручка по услугам (BasePrice * NormHour).</summary>
    public decimal ServicesRevenue { get; set; }

    /// <summary>Выручка по запчастям (Quantity * PricePerUnit).</summary>
    public decimal PartsRevenue { get; set; }

    /// <summary>Итоговая выручка.</summary>
    public decimal TotalRevenue { get; set; }
}
