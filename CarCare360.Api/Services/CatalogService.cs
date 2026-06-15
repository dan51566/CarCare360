using CarCare360.Api.Data;
using CarCare360.Api.Helpers;
using CarCare360.Api.Models.Dtos;
using CarCare360.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarCare360.Api.Services;

/// <summary>Реализация справочников услуг и запчастей.</summary>
public class CatalogService : ICatalogService
{
    private readonly CarCareDbContext _db;

    public CatalogService(CarCareDbContext db) => _db = db;

    // ===== Услуги =====

    /// <inheritdoc />
    public async Task<List<ServiceDto>> GetServicesAsync()
        => (await _db.Services.OrderBy(s => s.Name).ToListAsync()).Select(s => s.ToDto()).ToList();

    /// <inheritdoc />
    public async Task<ServiceDto> GetServiceAsync(int id)
    {
        var s = await _db.Services.FindAsync(id) ?? throw ApiException.NotFound("Услуга не найдена.");
        return s.ToDto();
    }

    /// <inheritdoc />
    public async Task<ServiceDto> CreateServiceAsync(ServiceUpsertRequest request)
    {
        var entity = new Service
        {
            Name = request.Name,
            Description = request.Description,
            NormHour = request.NormHour,
            BasePrice = request.BasePrice
        };
        _db.Services.Add(entity);
        await _db.SaveChangesAsync();
        return entity.ToDto();
    }

    /// <inheritdoc />
    public async Task<ServiceDto> UpdateServiceAsync(int id, ServiceUpsertRequest request)
    {
        var entity = await _db.Services.FindAsync(id) ?? throw ApiException.NotFound("Услуга не найдена.");
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.NormHour = request.NormHour;
        entity.BasePrice = request.BasePrice;
        await _db.SaveChangesAsync();
        return entity.ToDto();
    }

    /// <inheritdoc />
    public async Task DeleteServiceAsync(int id)
    {
        var entity = await _db.Services.FindAsync(id) ?? throw ApiException.NotFound("Услуга не найдена.");

        // В таблице Services нет флага удаления; запрещаем удаление, если услуга использована
        if (await _db.OrderServices.AnyAsync(os => os.ServiceID == id))
            throw ApiException.Conflict("Нельзя удалить услугу, использованную в заказах.");

        _db.Services.Remove(entity);
        await _db.SaveChangesAsync();
    }

    // ===== Справочник марок/моделей =====

    /// <inheritdoc />
    public async Task<List<CarModelDto>> GetCarModelsAsync()
        => (await _db.CarModels
                .AsNoTracking()
                .Include(m => m.Brand)
                .OrderBy(m => m.Brand!.Name)
                .ThenBy(m => m.Name)
                .ToListAsync())
            .Select(m => m.ToDto())
            .ToList();

    // ===== Запчасти =====

    /// <inheritdoc />
    public async Task<List<PartDto>> GetPartsAsync()
        => (await _db.Parts.OrderBy(p => p.Name).ToListAsync()).Select(p => p.ToDto()).ToList();

    /// <inheritdoc />
    public async Task<PartDto> GetPartAsync(int id)
    {
        var p = await _db.Parts.FindAsync(id) ?? throw ApiException.NotFound("Запчасть не найдена.");
        return p.ToDto();
    }

    /// <inheritdoc />
    public async Task<PartDto> CreatePartAsync(PartUpsertRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.PartNumber) &&
            await _db.Parts.AnyAsync(p => p.PartNumber == request.PartNumber))
            throw ApiException.Conflict("Запчасть с таким артикулом уже существует.");

        var entity = new Part
        {
            Name = request.Name,
            PartNumber = request.PartNumber,
            QuantityInStock = request.QuantityInStock ?? 0,
            Price = request.Price
        };
        _db.Parts.Add(entity);
        await _db.SaveChangesAsync();
        return entity.ToDto();
    }

    /// <inheritdoc />
    public async Task<PartDto> UpdatePartAsync(int id, PartUpsertRequest request)
    {
        var entity = await _db.Parts.FindAsync(id) ?? throw ApiException.NotFound("Запчасть не найдена.");

        if (!string.IsNullOrWhiteSpace(request.PartNumber) &&
            await _db.Parts.AnyAsync(p => p.PartNumber == request.PartNumber && p.PartID != id))
            throw ApiException.Conflict("Запчасть с таким артикулом уже существует.");

        entity.Name = request.Name;
        entity.PartNumber = request.PartNumber;
        entity.QuantityInStock = request.QuantityInStock ?? entity.QuantityInStock;
        entity.Price = request.Price;
        await _db.SaveChangesAsync();
        return entity.ToDto();
    }

    /// <inheritdoc />
    public async Task DeletePartAsync(int id)
    {
        var entity = await _db.Parts.FindAsync(id) ?? throw ApiException.NotFound("Запчасть не найдена.");

        if (await _db.OrderParts.AnyAsync(op => op.PartID == id))
            throw ApiException.Conflict("Нельзя удалить запчасть, использованную в заказах.");

        _db.Parts.Remove(entity);
        await _db.SaveChangesAsync();
    }
}
