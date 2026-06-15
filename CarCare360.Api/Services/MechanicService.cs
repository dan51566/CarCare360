using CarCare360.Api.Data;
using CarCare360.Api.Helpers;
using CarCare360.Api.Models.Dtos;
using CarCare360.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarCare360.Api.Services;

/// <summary>Реализация управления механиками.</summary>
public class MechanicService : IMechanicService
{
    private readonly CarCareDbContext _db;
    private readonly ILogger<MechanicService> _logger;

    public MechanicService(CarCareDbContext db, ILogger<MechanicService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<MechanicDto>> GetAllAsync()
    {
        var mechanics = await _db.Mechanics
            .Include(m => m.User)
            .Include(m => m.Specialization)
            .OrderBy(m => m.User!.FullName)
            .ToListAsync();

        return mechanics.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<MechanicDto> CreateAsync(MechanicCreateRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Login == request.Login))
            throw ApiException.Conflict("Пользователь с таким логином уже существует.");

        var mechanicRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == Roles.Mechanic)
            ?? throw ApiException.BadRequest("Роль «Механик» не найдена в БД.");

        if (request.SpecializationID.HasValue &&
            !await _db.Specializations.AnyAsync(s => s.SpecID == request.SpecializationID.Value))
            throw ApiException.BadRequest("Указанная специализация не найдена.");

        // 1) Создаём учётную запись пользователя-механика
        var user = new User
        {
            Login = request.Login,
            PasswordHash = PasswordHelper.Hash(request.Password),
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            RoleID = mechanicRole.RoleID,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(); // получаем UserID

        // 2) Создаём профиль механика
        var mechanic = new Mechanic
        {
            UserID = user.UserID,
            SpecializationID = request.SpecializationID,
            HireDate = request.HireDate,
            QualificationLevel = request.QualificationLevel
        };
        _db.Mechanics.Add(mechanic);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Создан механик {Login} (UserID={UserId}, MechanicID={MechanicId})",
            user.Login, user.UserID, mechanic.MechanicID);

        // Перезагружаем со связями для корректного DTO
        await _db.Entry(mechanic).Reference(m => m.User).LoadAsync();
        await _db.Entry(mechanic).Reference(m => m.Specialization).LoadAsync();
        return MapToDto(mechanic);
    }

    /// <inheritdoc />
    public async Task<List<MechanicScheduleItemDto>> GetScheduleAsync(int mechanicId)
    {
        if (!await _db.Mechanics.AnyAsync(m => m.MechanicID == mechanicId))
            throw ApiException.NotFound("Механик не найден.");

        var items = await _db.OrderServices
            .Include(os => os.Service)
            .Include(os => os.Order).ThenInclude(o => o!.Client)
            .Include(os => os.Order).ThenInclude(o => o!.Car).ThenInclude(c => c!.Model).ThenInclude(m => m!.Brand)
            .Where(os => os.MechanicID == mechanicId && os.Order!.IsDeleted != true)
            .OrderByDescending(os => os.Order!.OrderID)
            .ToListAsync();

        return items.Select(os => new MechanicScheduleItemDto
        {
            OrderServiceID = os.OrderServiceID,
            OrderID = os.OrderID,
            ServiceName = os.Service?.Name,
            Status = os.Status,
            StartTime = os.StartTime,
            EndTime = os.EndTime,
            ScheduledDate = os.Order?.ScheduledDate,
            CarInfo = os.Order?.Car?.CarInfo(),
            ClientName = os.Order?.Client?.FullName
        }).ToList();
    }

    private static MechanicDto MapToDto(Mechanic m) => new()
    {
        MechanicID = m.MechanicID,
        UserID = m.UserID,
        FullName = m.User?.FullName ?? string.Empty,
        Login = m.User?.Login ?? string.Empty,
        Email = m.User?.Email,
        Phone = m.User?.Phone,
        SpecializationID = m.SpecializationID,
        SpecializationName = m.Specialization?.Name,
        HireDate = m.HireDate,
        QualificationLevel = m.QualificationLevel,
        IsActive = m.User?.IsActive ?? false
    };
}
