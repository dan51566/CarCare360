using CarCare360.Api.Data;
using CarCare360.Api.Helpers;
using CarCare360.Api.Models.Dtos;
using CarCare360.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarCare360.Api.Services;

/// <summary>Реализация управления автомобилями с проверкой владельца.</summary>
public class CarService : ICarService
{
    private readonly CarCareDbContext _db;

    public CarService(CarCareDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<List<CarDto>> GetAllAsync(CurrentUser current)
    {
        var query = _db.Cars
            .Include(c => c.Client)
            .Include(c => c.Model).ThenInclude(m => m!.Brand)
            .AsQueryable();

        // Клиент видит только свои автомобили
        if (!current.IsAdmin)
            query = query.Where(c => c.ClientID == current.UserId);

        var cars = await query.OrderBy(c => c.CarID).ToListAsync();
        return cars.Select(c => c.ToDto()).ToList();
    }

    /// <inheritdoc />
    public async Task<CarDto> GetByIdAsync(int id, CurrentUser current)
    {
        var car = await LoadCarAsync(id);
        EnsureOwnerOrAdmin(car, current);
        return car.ToDto();
    }

    /// <inheritdoc />
    public async Task<CarDto> CreateAsync(CarCreateRequest request, CurrentUser current)
    {
        // Проверяем существование модели
        if (!await _db.CarModels.AnyAsync(m => m.ModelID == request.ModelID))
            throw ApiException.BadRequest("Указанная модель автомобиля не найдена.");

        // Клиент привязывает авто к себе; админ может указать клиента явно
        var clientId = current.IsAdmin && request.ClientID.HasValue
            ? request.ClientID.Value
            : current.UserId;

        if (current.IsAdmin && !await _db.Users.AnyAsync(u => u.UserID == clientId && u.IsDeleted != true))
            throw ApiException.BadRequest("Указанный клиент не найден.");

        var car = new Car
        {
            ClientID = clientId,
            ModelID = request.ModelID,
            Year = request.Year,
            VIN = request.VIN,
            LicensePlate = request.LicensePlate,
            Color = request.Color,
            Mileage = request.Mileage
        };

        _db.Cars.Add(car);
        await _db.SaveChangesAsync();

        return (await LoadCarAsync(car.CarID)).ToDto();
    }

    /// <inheritdoc />
    public async Task<CarDto> UpdateAsync(int id, CarUpdateRequest request, CurrentUser current)
    {
        var car = await LoadCarAsync(id);
        EnsureOwnerOrAdmin(car, current);

        if (!await _db.CarModels.AnyAsync(m => m.ModelID == request.ModelID))
            throw ApiException.BadRequest("Указанная модель автомобиля не найдена.");

        car.ModelID = request.ModelID;
        car.Year = request.Year;
        car.VIN = request.VIN;
        car.LicensePlate = request.LicensePlate;
        car.Color = request.Color;
        car.Mileage = request.Mileage;
        await _db.SaveChangesAsync();

        return (await LoadCarAsync(car.CarID)).ToDto();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CurrentUser current)
    {
        var car = await _db.Cars.FirstOrDefaultAsync(c => c.CarID == id)
            ?? throw ApiException.NotFound("Автомобиль не найден.");
        EnsureOwnerOrAdmin(car, current);

        // В таблице Cars нет столбца мягкого удаления; нельзя удалять авто с заказами
        if (await _db.Orders.AnyAsync(o => o.CarID == id))
            throw ApiException.Conflict("Нельзя удалить автомобиль, по которому есть заказы.");

        _db.Cars.Remove(car);
        await _db.SaveChangesAsync();
    }

    private async Task<Car> LoadCarAsync(int id)
        => await _db.Cars
            .Include(c => c.Client)
            .Include(c => c.Model).ThenInclude(m => m!.Brand)
            .FirstOrDefaultAsync(c => c.CarID == id)
            ?? throw ApiException.NotFound("Автомобиль не найден.");

    private static void EnsureOwnerOrAdmin(Car car, CurrentUser current)
    {
        if (!current.IsAdmin && car.ClientID != current.UserId)
            throw ApiException.Forbidden("Доступ к чужому автомобилю запрещён.");
    }
}
