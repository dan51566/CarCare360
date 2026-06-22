using CarCare360.Api.Data;
using CarCare360.Api.Helpers;
using CarCare360.Api.Models.Dtos;
using CarCare360.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarCare360.Api.Services;

/// <summary>Реализация избранных механиков клиента (Изменение №2, Доработка 3).</summary>
public class FavoriteMechanicService : IFavoriteMechanicService
{
    private readonly CarCareDbContext _db;
    private readonly ILogger<FavoriteMechanicService> _logger;

    public FavoriteMechanicService(CarCareDbContext db, ILogger<FavoriteMechanicService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<MechanicCatalogDto>> GetCatalogAsync(int userId)
    {
        // Идентификаторы избранных механиков текущего клиента.
        var favoriteIds = (await _db.FavoriteMechanics
            .Where(f => f.UserID == userId)
            .Select(f => f.MechanicID)
            .ToListAsync()).ToHashSet();

        // Только активные механики (деактивированные — Users.IsActive=0 / IsDeleted=1 — исключаем).
        var mechanics = await _db.Mechanics
            .Include(m => m.User)
            .Include(m => m.Specialization)
            .Where(m => m.User!.IsActive == true && m.User.IsDeleted != true)
            .OrderBy(m => m.User!.FullName)
            .ToListAsync();

        return mechanics
            .Select(m => new MechanicCatalogDto
            {
                MechanicID = m.MechanicID,
                FullName = m.User?.FullName ?? string.Empty,
                SpecializationName = m.Specialization?.Name,
                QualificationLevel = m.QualificationLevel,
                IsFavorite = favoriteIds.Contains(m.MechanicID)
            })
            // Избранные — первыми; внутри группы сохраняем алфавитный порядок.
            .OrderByDescending(d => d.IsFavorite)
            .ThenBy(d => d.FullName)
            .ToList();
    }

    /// <inheritdoc />
    public async Task AddAsync(int userId, int mechanicId)
    {
        // Механик должен существовать и быть активным.
        var isActiveMechanic = await _db.Mechanics
            .AnyAsync(m => m.MechanicID == mechanicId
                        && m.User!.IsActive == true
                        && m.User.IsDeleted != true);
        if (!isActiveMechanic)
            throw ApiException.NotFound("Механик не найден или неактивен.");

        // Идемпотентно: если уже в избранном — ничего не делаем (уникальность UQ_FavoriteMechanics).
        if (await _db.FavoriteMechanics.AnyAsync(f => f.UserID == userId && f.MechanicID == mechanicId))
            return;

        _db.FavoriteMechanics.Add(new FavoriteMechanic
        {
            UserID = userId,
            MechanicID = mechanicId,
            AddedAt = DateTime.Now
        });
        await _db.SaveChangesAsync();
        _logger.LogInformation("Клиент {UserId} добавил механика {MechanicId} в избранное.", userId, mechanicId);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(int userId, int mechanicId)
    {
        var favorite = await _db.FavoriteMechanics
            .FirstOrDefaultAsync(f => f.UserID == userId && f.MechanicID == mechanicId);
        if (favorite is null)
            return; // DELETE идемпотентен — если записи нет, считаем успехом

        _db.FavoriteMechanics.Remove(favorite);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Клиент {UserId} убрал механика {MechanicId} из избранного.", userId, mechanicId);
    }
}
