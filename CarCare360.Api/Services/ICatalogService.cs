using CarCare360.Api.Models.Dtos;

namespace CarCare360.Api.Services;

/// <summary>
/// Сервис справочников: услуги (Services) и запчасти (Parts).
/// </summary>
public interface ICatalogService
{
    // ── Услуги ──
    Task<List<ServiceDto>> GetServicesAsync();
    Task<ServiceDto> GetServiceAsync(int id);
    Task<ServiceDto> CreateServiceAsync(ServiceUpsertRequest request);
    Task<ServiceDto> UpdateServiceAsync(int id, ServiceUpsertRequest request);
    Task DeleteServiceAsync(int id);

    // ── Справочник марок/моделей (для мобильного приложения) ──
    Task<List<CarModelDto>> GetCarModelsAsync();

    // ── Запчасти ──
    Task<List<PartDto>> GetPartsAsync();
    Task<PartDto> GetPartAsync(int id);
    Task<PartDto> CreatePartAsync(PartUpsertRequest request);
    Task<PartDto> UpdatePartAsync(int id, PartUpsertRequest request);
    Task DeletePartAsync(int id);
}
