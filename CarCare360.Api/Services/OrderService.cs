using System.Data;
using System.Globalization;
using CarCare360.Api.Data;
using CarCare360.Api.Helpers;
using CarCare360.Api.Models.Dtos;
using CarCare360.Api.Models.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CarCare360.Api.Services;

/// <summary>
/// Реализация управления заказами.
/// Создание заказа и добавление услуг выполняются через хранимые процедуры,
/// изменение статуса — через UpdateOrderStatus. Доступ разграничен по ролям.
/// </summary>
public class OrderService : IOrderService
{
    private readonly CarCareDbContext _db;
    private readonly ILogger<OrderService> _logger;

    public OrderService(CarCareDbContext db, ILogger<OrderService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<OrderDto>> GetAllAsync(CurrentUser current)
    {
        var query = BaseQuery().Where(o => o.IsDeleted != true);

        if (current.IsClient)
        {
            query = query.Where(o => o.ClientID == current.UserId);
        }
        else if (current.IsMechanic)
        {
            // Механику видны заказы, в которых есть назначенная ему услуга
            var mechanicId = await GetMechanicIdAsync(current.UserId);
            query = query.Where(o => o.OrderServices.Any(os => os.MechanicID == mechanicId));
        }
        // Администратор видит все заказы

        var orders = await query.OrderByDescending(o => o.OrderID).ToListAsync();
        return orders.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<OrderDto> GetByIdAsync(int id, CurrentUser current)
    {
        var order = await LoadOrderAsync(id);
        await EnsureAccessAsync(order, current);
        return MapToDto(order);
    }

    /// <inheritdoc />
    public async Task<OrderDto> CreateAsync(OrderCreateRequest request, CurrentUser current)
    {
        // Проверяем автомобиль и определяем владельца
        var car = await _db.Cars.FirstOrDefaultAsync(c => c.CarID == request.CarID)
            ?? throw ApiException.BadRequest("Автомобиль не найден.");

        // Клиент может создавать заказ только по своему автомобилю
        if (current.IsClient && car.ClientID != current.UserId)
            throw ApiException.Forbidden("Нельзя создать заказ по чужому автомобилю.");

        var clientId = car.ClientID; // заказ оформляется на владельца автомобиля

        // Проверяем услуги
        if (request.ServiceIds.Count > 0)
        {
            var existing = await _db.Services
                .Where(s => request.ServiceIds.Contains(s.ServiceID))
                .Select(s => s.ServiceID)
                .ToListAsync();

            var missing = request.ServiceIds.Except(existing).ToList();
            if (missing.Count > 0)
                throw ApiException.BadRequest($"Услуги не найдены: {string.Join(", ", missing)}.");
        }

        var scheduledTime = ParseTime(request.ScheduledTime);

        // ── Хранимая процедура CreateOrder → возвращает NewOrderID ──
        var newOrderId = await ExecuteSpAsync(() => _db.ExecuteScalarIntAsync(
            "CreateOrder",
            new SqlParameter("@CarID", request.CarID),
            new SqlParameter("@ClientID", clientId),
            new SqlParameter("@ScheduledDate", SqlDbType.Date)
                { Value = (object?)request.ScheduledDate?.Date ?? DBNull.Value },
            new SqlParameter("@ScheduledTime", SqlDbType.Time)
                { Value = (object?)scheduledTime ?? DBNull.Value },
            new SqlParameter("@Notes", (object?)request.Notes ?? DBNull.Value)));

        // ── Добавляем услуги через AddServiceToOrder ──
        foreach (var serviceId in request.ServiceIds.Distinct())
        {
            await ExecuteSpAsync(() => _db.ExecuteNonQueryAsync(
                "AddServiceToOrder",
                new SqlParameter("@OrderID", newOrderId),
                new SqlParameter("@ServiceID", serviceId),
                new SqlParameter("@MechanicID", DBNull.Value),
                new SqlParameter("@BayID", DBNull.Value)));
        }

        _logger.LogInformation("Создан заказ #{OrderId} клиентом {ClientId}", newOrderId, clientId);
        return MapToDto(await LoadOrderAsync(newOrderId));
    }

    /// <inheritdoc />
    public async Task<OrderDto> UpdateStatusAsync(int id, string newStatus, CurrentUser current)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderID == id && o.IsDeleted != true)
            ?? throw ApiException.NotFound("Заказ не найден.");

        if (!OrderStatuses.IsValid(newStatus))
            throw ApiException.BadRequest(
                $"Недопустимый статус. Разрешены: {string.Join(", ", OrderStatuses.All)}.");

        // Механик может менять статус только своих заказов
        if (current.IsMechanic)
        {
            var mechanicId = await GetMechanicIdAsync(current.UserId);
            var assigned = await _db.OrderServices.AnyAsync(os => os.OrderID == id && os.MechanicID == mechanicId);
            if (!assigned)
                throw ApiException.Forbidden("Заказ не назначен текущему механику.");
        }

        // ── Хранимая процедура UpdateOrderStatus (внутри — проверка и RAISERROR) ──
        await ExecuteSpAsync(() => _db.ExecuteNonQueryAsync(
            "UpdateOrderStatus",
            new SqlParameter("@OrderID", id),
            new SqlParameter("@NewStatus", newStatus)));

        return MapToDto(await LoadOrderAsync(id));
    }

    /// <inheritdoc />
    public async Task<OrderDto> UpdateAsync(int id, OrderUpdateRequest request, CurrentUser current)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderID == id && o.IsDeleted != true)
            ?? throw ApiException.NotFound("Заказ не найден.");

        if (current.IsClient)
        {
            // Клиент может менять только примечание и только в своём заказе
            if (order.ClientID != current.UserId)
                throw ApiException.Forbidden("Доступ к чужому заказу запрещён.");
            order.Notes = request.Notes;
        }
        else if (current.IsAdmin)
        {
            order.Notes = request.Notes;
            order.ScheduledDate = request.ScheduledDate;
            order.ScheduledTime = ParseTime(request.ScheduledTime);
            order.Mileage = request.Mileage;
        }
        else
        {
            throw ApiException.Forbidden("Недостаточно прав для обновления заказа.");
        }

        await _db.SaveChangesAsync();
        return MapToDto(await LoadOrderAsync(id));
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CurrentUser current)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderID == id && o.IsDeleted != true)
            ?? throw ApiException.NotFound("Заказ не найден.");

        // Клиент может удалить только свой заказ
        if (current.IsClient && order.ClientID != current.UserId)
            throw ApiException.Forbidden("Доступ к чужому заказу запрещён.");
        if (current.IsMechanic)
            throw ApiException.Forbidden("Механик не может удалять заказы.");

        order.IsDeleted = true; // мягкое удаление
        await _db.SaveChangesAsync();
    }

    // ===== Вспомогательные методы =====

    // AsNoTracking: запросы только для чтения (маппинг в DTO). Также гарантирует
    // чтение свежих значений из БД после изменений через хранимые процедуры
    // (иначе identity map EF мог бы вернуть устаревший отслеживаемый экземпляр).
    private IQueryable<Order> BaseQuery() => _db.Orders
        .AsNoTracking()
        .Include(o => o.Client)
        .Include(o => o.Car).ThenInclude(c => c!.Model).ThenInclude(m => m!.Brand)
        .Include(o => o.OrderServices).ThenInclude(os => os.Service)
        .Include(o => o.OrderServices).ThenInclude(os => os.Mechanic).ThenInclude(m => m!.User)
        .Include(o => o.OrderServices).ThenInclude(os => os.Bay)
        .Include(o => o.OrderParts).ThenInclude(op => op.Part)
        .AsSplitQuery();

    private async Task<Order> LoadOrderAsync(int id)
        => await BaseQuery().FirstOrDefaultAsync(o => o.OrderID == id && o.IsDeleted != true)
            ?? throw ApiException.NotFound("Заказ не найден.");

    /// <summary>Возвращает MechanicID по UserID текущего пользователя-механика.</summary>
    private async Task<int> GetMechanicIdAsync(int userId)
        => await _db.Mechanics.Where(m => m.UserID == userId).Select(m => m.MechanicID).FirstOrDefaultAsync();

    private async Task EnsureAccessAsync(Order order, CurrentUser current)
    {
        if (current.IsAdmin) return;
        if (current.IsClient)
        {
            if (order.ClientID != current.UserId)
                throw ApiException.Forbidden("Доступ к чужому заказу запрещён.");
            return;
        }
        if (current.IsMechanic)
        {
            var mechanicId = await GetMechanicIdAsync(current.UserId);
            if (!order.OrderServices.Any(os => os.MechanicID == mechanicId))
                throw ApiException.Forbidden("Заказ не назначен текущему механику.");
        }
    }

    /// <summary>Парсит время вида "HH:mm" / "HH:mm:ss" в TimeSpan.</summary>
    private static TimeSpan? ParseTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var ts)) return ts;
        throw ApiException.BadRequest("Неверный формат времени. Ожидается \"ЧЧ:ММ\".");
    }

    /// <summary>
    /// Выполняет вызов хранимой процедуры, переводя ошибки SQL (RAISERROR/THROW)
    /// в понятный HTTP-ответ 400.
    /// </summary>
    private static async Task<T> ExecuteSpAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (SqlException ex)
        {
            throw ApiException.BadRequest(ex.Message);
        }
    }

    private static async Task ExecuteSpAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (SqlException ex)
        {
            throw ApiException.BadRequest(ex.Message);
        }
    }

    /// <summary>Маппинг заказа в детальный DTO.</summary>
    private static OrderDto MapToDto(Order o)
    {
        var dto = new OrderDto
        {
            OrderID = o.OrderID,
            CarID = o.CarID,
            CarInfo = o.Car?.CarInfo(),
            ClientID = o.ClientID,
            ClientName = o.Client?.FullName,
            CreatedAt = o.CreatedAt,
            ScheduledDate = o.ScheduledDate,
            ScheduledTime = o.ScheduledTime?.ToString(@"hh\:mm"),
            Status = o.Status,
            Mileage = o.Mileage,
            Notes = o.Notes,
            Services = o.OrderServices.Select(os => new OrderServiceDto
            {
                OrderServiceID = os.OrderServiceID,
                ServiceID = os.ServiceID,
                ServiceName = os.Service?.Name,
                MechanicID = os.MechanicID,
                MechanicName = os.Mechanic?.User?.FullName,
                BayID = os.BayID,
                BayName = os.Bay?.Name,
                StartTime = os.StartTime,
                EndTime = os.EndTime,
                Status = os.Status
            }).ToList(),
            Parts = o.OrderParts.Select(op => new OrderPartDto
            {
                OrderPartID = op.OrderPartID,
                PartID = op.PartID,
                PartName = op.Part?.Name,
                Quantity = op.Quantity,
                PricePerUnit = op.PricePerUnit
            }).ToList()
        };

        dto.PartsTotal = dto.Parts.Sum(p => (p.PricePerUnit ?? 0) * p.Quantity);
        return dto;
    }
}
