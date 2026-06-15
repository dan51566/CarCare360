using CarCare360.Api.Helpers;
using CarCare360.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarCare360.Api.Data;

/// <summary>
/// Идемпотентный засевщик тестовых данных для API (только в среде Development).
/// Учётные записи admin/admin123 и mechanic/mechanic123 создаются десктопом;
/// здесь добавляется тестовый клиент client/client123, если его ещё нет.
/// Схема БД не изменяется.
/// </summary>
public static class ApiSeeder
{
    public static async Task SeedAsync(CarCareDbContext db, ILogger logger)
    {
        var clientRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == Roles.Client);
        if (clientRole is null)
        {
            logger.LogWarning("Роль «Клиент» не найдена — пропуск засева тестового клиента.");
            return;
        }

        if (await db.Users.AnyAsync(u => u.Login == "client"))
            return;

        db.Users.Add(new User
        {
            Login = "client",
            PasswordHash = PasswordHelper.Hash("client123"),
            FullName = "Клиент Тестовый",
            Email = "client@example.com",
            Phone = "+7 900 000-00-00",
            RoleID = clientRole.RoleID,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        logger.LogInformation("Создан тестовый клиент client/client123.");
    }
}
