using CarCare360.Api.Data;
using CarCare360.Api.Helpers;
using CarCare360.Api.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CarCare360.Api.Services;

/// <summary>Реализация аналитических отчётов.</summary>
public class ReportService : IReportService
{
    private readonly CarCareDbContext _db;

    public ReportService(CarCareDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<List<MechanicsLoadReportDto>> GetMechanicsLoadAsync(DateTime from, DateTime to)
    {
        ValidatePeriod(from, to);
        var toExclusive = to.Date.AddDays(1); // включаем весь день «to»

        // Услуги механиков по заказам, созданным в периоде
        var data = await _db.OrderServices
            .Where(os => os.MechanicID != null
                         && os.Order!.IsDeleted != true
                         && os.Order.CreatedAt >= from.Date
                         && os.Order.CreatedAt < toExclusive)
            .Select(os => new
            {
                os.MechanicID,
                MechanicName = os.Mechanic!.User!.FullName,
                Specialization = os.Mechanic.Specialization!.Name,
                NormHour = os.Service!.NormHour,
                IsCompleted = os.Status == "Выполнена"
            })
            .ToListAsync();

        return data
            .GroupBy(x => new { x.MechanicID, x.MechanicName, x.Specialization })
            .Select(g => new MechanicsLoadReportDto
            {
                MechanicID = g.Key.MechanicID ?? 0,
                MechanicName = g.Key.MechanicName,
                SpecializationName = g.Key.Specialization,
                TotalServices = g.Count(),
                CompletedServices = g.Count(x => x.IsCompleted),
                TotalNormHours = g.Sum(x => x.NormHour)
            })
            .OrderByDescending(r => r.TotalServices)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<FinancialReportDto> GetFinancialAsync(DateTime from, DateTime to)
    {
        ValidatePeriod(from, to);
        var toExclusive = to.Date.AddDays(1);

        // Заказы периода (не удалённые)
        var orderIds = await _db.Orders
            .Where(o => o.IsDeleted != true && o.CreatedAt >= from.Date && o.CreatedAt < toExclusive)
            .Select(o => o.OrderID)
            .ToListAsync();

        // Выручка по услугам: BasePrice * NormHour
        var servicesRevenue = await _db.OrderServices
            .Where(os => orderIds.Contains(os.OrderID))
            .SumAsync(os => (decimal?)((os.Service!.BasePrice ?? 0) * os.Service.NormHour)) ?? 0m;

        // Выручка по запчастям: Quantity * PricePerUnit
        var partsRevenue = await _db.OrderParts
            .Where(op => orderIds.Contains(op.OrderID))
            .SumAsync(op => (decimal?)((op.PricePerUnit ?? 0) * op.Quantity)) ?? 0m;

        return new FinancialReportDto
        {
            FromDate = from.Date,
            ToDate = to.Date,
            OrdersCount = orderIds.Count,
            ServicesRevenue = servicesRevenue,
            PartsRevenue = partsRevenue,
            TotalRevenue = servicesRevenue + partsRevenue
        };
    }

    private static void ValidatePeriod(DateTime from, DateTime to)
    {
        if (from > to)
            throw ApiException.BadRequest("Начало периода не может быть позже конца.");
    }
}
