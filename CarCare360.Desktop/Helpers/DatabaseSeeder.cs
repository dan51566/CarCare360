using CarCare360.Desktop.Data;
using CarCare360.Desktop.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CarCare360.Desktop.Helpers;

/// <summary>
/// Засевщик начальных данных.
/// При первом запуске создаёт тестовых пользователей (Администратор и Механик),
/// если они ещё не существуют в БД.
///
/// Особенность схемы:
///  PasswordHash — BINARY(64): BCrypt-хеш хранится как ASCII-байты,
///  дополненные нулями до 64 байт.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Конвертирует BCrypt-строку (60 символов ASCII) в byte[64] для хранения в BINARY(64).
    /// </summary>
    public static byte[] HashToBytes(string bcryptHash)
    {
        var bytes = new byte[64];
        var ascii = Encoding.ASCII.GetBytes(bcryptHash);
        // Копируем хеш (60 байт) в начало буфера; остаток заполняется нулями
        Array.Copy(ascii, bytes, Math.Min(ascii.Length, 64));
        return bytes;
    }

    /// <summary>
    /// Конвертирует byte[64] обратно в BCrypt-строку для вызова BCrypt.Verify().
    /// </summary>
    public static string BytesToHash(byte[] bytes)
        => Encoding.ASCII.GetString(bytes).TrimEnd('\0');

    /// <summary>
    /// Проверяет и при необходимости создаёт тестовых пользователей в БД.
    /// Метод идемпотентен — при повторном вызове ничего не дублирует.
    /// </summary>
    public static async Task SeedAsync(CarCareDbContext db)
    {
        // ── Шаг 1: Убеждаемся, что роли существуют ────────────────────────
        var adminRole    = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Администратор");
        var mechanicRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Механик");

        if (adminRole is null || mechanicRole is null)
        {
            // Роли должны быть созданы вместе с БД; если их нет — предупреждаем
            throw new InvalidOperationException(
                "Роли 'Администратор' и 'Механик' не найдены в БД. " +
                "Убедитесь, что таблица Roles заполнена корректно.");
        }

        bool changed = false;

        // ── Шаг 2: Тестовый администратор (admin / admin123) ───────────────
        if (!await db.Users.AnyAsync(u => u.Login == "admin"))
        {
            db.Users.Add(new User
            {
                Login        = "admin",
                PasswordHash = HashToBytes(
                    BCrypt.Net.BCrypt.HashPassword("admin123", workFactor: 12)),
                FullName     = "Администратор",
                RoleID       = adminRole.RoleID,
                IsActive     = true,
                CreatedAt    = DateTime.UtcNow
            });
            changed = true;
        }

        // ── Шаг 3: Тестовый механик (mechanic / mechanic123) ──────────────
        if (!await db.Users.AnyAsync(u => u.Login == "mechanic"))
        {
            db.Users.Add(new User
            {
                Login        = "mechanic",
                PasswordHash = HashToBytes(
                    BCrypt.Net.BCrypt.HashPassword("mechanic123", workFactor: 12)),
                FullName     = "Механик Тестовый",
                RoleID       = mechanicRole.RoleID,
                IsActive     = true,
                CreatedAt    = DateTime.UtcNow
            });
            changed = true;
        }

        if (changed)
            await db.SaveChangesAsync();

        // ── Шаг 4: Марки и модели автомобилей ────────────────────────────────
        await SeedCarBrandsAsync(db);

        // ── Шаг 5: Боксы, услуги, специализации, механик ─────────────────────
        await SeedServiceDataAsync(db);
    }

    /// <summary>
    /// Засевает боксы, услуги, специализации и профиль тестового механика.
    /// Идемпотентен — пропускает уже существующие записи.
    /// </summary>
    private static async Task SeedServiceDataAsync(CarCareDbContext db)
    {
        // Боксы (рабочие посты)
        if (!await db.ServiceBays.AnyAsync())
        {
            db.ServiceBays.AddRange(
                new ServiceBay { Name = "Бокс 1", IsActive = true },
                new ServiceBay { Name = "Бокс 2", IsActive = true },
                new ServiceBay { Name = "Бокс 3", IsActive = true },
                new ServiceBay { Name = "Бокс 4 (шиномонтаж)", IsActive = true });
            await db.SaveChangesAsync();
        }

        // Специализации механиков
        if (!await db.Specializations.AnyAsync())
        {
            db.Specializations.AddRange(
                new Specialization { Name = "Слесарь-механик" },
                new Specialization { Name = "Диагност" },
                new Specialization { Name = "Электрик" },
                new Specialization { Name = "Кузовщик" },
                new Specialization { Name = "Шиномонтажник" });
            await db.SaveChangesAsync();
        }

        // Услуги
        if (!await db.Services.AnyAsync())
        {
            db.Services.AddRange(
                new Service { Name = "Замена масла",                NormHour = 0.5m, BasePrice =  800m  },
                new Service { Name = "Диагностика двигателя",       NormHour = 1.0m, BasePrice = 1500m  },
                new Service { Name = "Замена воздушного фильтра",   NormHour = 0.3m, BasePrice =  400m  },
                new Service { Name = "Замена тормозных колодок",    NormHour = 1.5m, BasePrice = 2500m  },
                new Service { Name = "Шиномонтаж (4 шины)",         NormHour = 0.5m, BasePrice = 1200m  },
                new Service { Name = "Балансировка колёс",          NormHour = 0.5m, BasePrice =  800m  },
                new Service { Name = "Замена ремня ГРМ",            NormHour = 3.0m, BasePrice = 5000m  },
                new Service { Name = "Промывка форсунок",           NormHour = 1.0m, BasePrice = 2000m  },
                new Service { Name = "Компьютерная диагностика",    NormHour = 1.0m, BasePrice = 1200m  },
                new Service { Name = "Техническое обслуживание",    NormHour = 2.0m, BasePrice = 3500m  });
            await db.SaveChangesAsync();
        }

        // Профиль механика для тестового пользователя "mechanic"
        var mechanicUser = await db.Users.FirstOrDefaultAsync(u => u.Login == "mechanic");
        if (mechanicUser is not null && !await db.Mechanics.AnyAsync(m => m.UserID == mechanicUser.UserID))
        {
            var spec = await db.Specializations.FirstOrDefaultAsync();
            db.Mechanics.Add(new Mechanic
            {
                UserID           = mechanicUser.UserID,
                SpecializationID = spec?.SpecID,
                HireDate         = new DateTime(2023, 1, 15),
                QualificationLevel = "Senior"
            });
            await db.SaveChangesAsync();
        }

        // Тестовые запчасти на складе
        if (!await db.Parts.AnyAsync())
        {
            db.Parts.AddRange(
                new Part { Name = "Масло моторное 5W-30 (4 л)",      PartNumber = "OIL-5W30-4L",   Price = 1200m, QuantityInStock = 50 },
                new Part { Name = "Масляный фильтр",                  PartNumber = "FLT-OIL-01",    Price =  350m, QuantityInStock = 80 },
                new Part { Name = "Воздушный фильтр",                 PartNumber = "FLT-AIR-01",    Price =  450m, QuantityInStock = 40 },
                new Part { Name = "Колодки тормозные передние (компл.)", PartNumber = "BRK-PAD-F",  Price = 1800m, QuantityInStock = 25 },
                new Part { Name = "Свечи зажигания (компл. 4 шт.)",  PartNumber = "SPARK-4",       Price =  900m, QuantityInStock = 30 },
                new Part { Name = "Ремень ГРМ",                        PartNumber = "BELT-GRM-01",   Price = 1500m, QuantityInStock = 15 },
                new Part { Name = "Антифриз (5 л)",                   PartNumber = "COOL-5L",       Price =  600m, QuantityInStock = 20 },
                new Part { Name = "Тормозная жидкость (DOT4, 0.5 л)", PartNumber = "BRK-FL-DOT4",  Price =  250m, QuantityInStock = 35 });
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Засевает справочник марок и моделей автомобилей.
    /// Идемпотентен — пропускает уже существующие записи.
    /// </summary>
    private static async Task SeedCarBrandsAsync(CarCareDbContext db)
    {
        if (await db.CarBrands.AnyAsync()) return; // уже засеяно

        // Данные: марка → список моделей
        var catalog = new Dictionary<(string Name, string Country), string[]>
        {
            [("Lada (ВАЗ)", "Россия")]     = ["Granta", "Vesta", "XRAY", "Niva Travel", "Largus"],
            [("Toyota", "Япония")]          = ["Camry", "Corolla", "RAV4", "Land Cruiser 200", "Highlander"],
            [("Kia", "Южная Корея")]        = ["Rio", "Ceed", "Sportage", "Sorento", "K5"],
            [("Hyundai", "Южная Корея")]    = ["Solaris", "Elantra", "Tucson", "Santa Fe", "Creta"],
            [("Volkswagen", "Германия")]    = ["Polo", "Golf", "Tiguan", "Passat", "Jetta"],
            [("BMW", "Германия")]           = ["3 Series", "5 Series", "X5", "X3", "7 Series"],
            [("Mercedes-Benz", "Германия")] = ["C-Class", "E-Class", "GLE", "GLC", "S-Class"],
            [("Ford", "США")]               = ["Focus", "Fiesta", "Explorer", "Kuga", "Ranger"],
            [("Nissan", "Япония")]          = ["Qashqai", "X-Trail", "Almera", "Pathfinder", "Teana"],
            [("Renault", "Франция")]        = ["Logan", "Sandero", "Duster", "Arkana", "Kaptur"],
        };

        foreach (var ((brandName, country), models) in catalog)
        {
            var brand = new CarBrand { Name = brandName, Country = country };
            db.CarBrands.Add(brand);
            await db.SaveChangesAsync(); // получаем BrandID

            foreach (var modelName in models)
            {
                db.CarModels.Add(new CarModel
                {
                    BrandID = brand.BrandID,
                    Name    = modelName
                });
            }
            await db.SaveChangesAsync();
        }
    }
}
