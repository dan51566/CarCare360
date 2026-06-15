using CarCare360.Api.Data;
using CarCare360.Api.Helpers;
using CarCare360.Api.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CarCare360.Api.Services;

/// <summary>Реализация управления клиентами.</summary>
public class ClientService : IClientService
{
    private readonly CarCareDbContext _db;

    public ClientService(CarCareDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<List<UserDto>> GetAllAsync()
    {
        var clients = await _db.Users
            .Include(u => u.Role)
            .Where(u => u.Role!.Name == Roles.Client && u.IsDeleted != true)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        return clients.Select(c => c.ToDto()).ToList();
    }

    /// <inheritdoc />
    public async Task<UserDto> GetByIdAsync(int id, CurrentUser current)
    {
        EnsureSelfOrAdmin(id, current);

        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserID == id && u.IsDeleted != true)
            ?? throw ApiException.NotFound("Клиент не найден.");

        return user.ToDto();
    }

    /// <inheritdoc />
    public async Task<UserDto> UpdateAsync(int id, ClientUpdateRequest request, CurrentUser current)
    {
        EnsureSelfOrAdmin(id, current);

        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserID == id && u.IsDeleted != true)
            ?? throw ApiException.NotFound("Клиент не найден.");

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.Phone = request.Phone;
        await _db.SaveChangesAsync();

        return user.ToDto();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == id && u.IsDeleted != true)
            ?? throw ApiException.NotFound("Клиент не найден.");

        user.IsDeleted = true;   // мягкое удаление
        user.IsActive = false;
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<List<CarDto>> GetCarsAsync(int id, CurrentUser current)
    {
        EnsureSelfOrAdmin(id, current);

        var cars = await _db.Cars
            .Include(c => c.Client)
            .Include(c => c.Model).ThenInclude(m => m!.Brand)
            .Where(c => c.ClientID == id)
            .ToListAsync();

        return cars.Select(c => c.ToDto()).ToList();
    }

    /// <summary>Проверка: доступ разрешён самому клиенту или администратору.</summary>
    private static void EnsureSelfOrAdmin(int id, CurrentUser current)
    {
        if (!current.IsAdmin && current.UserId != id)
            throw ApiException.Forbidden("Доступ к данным другого клиента запрещён.");
    }
}
