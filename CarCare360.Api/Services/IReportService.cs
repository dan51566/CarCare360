using CarCare360.Api.Models.Dtos;

namespace CarCare360.Api.Services;

/// <summary>Сервис отчётов (только для администратора).</summary>
public interface IReportService
{
    /// <summary>Загрузка механиков за период.</summary>
    Task<List<MechanicsLoadReportDto>> GetMechanicsLoadAsync(DateTime from, DateTime to);

    /// <summary>Финансовый отчёт за период.</summary>
    Task<FinancialReportDto> GetFinancialAsync(DateTime from, DateTime to);
}
