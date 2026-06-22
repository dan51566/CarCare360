using CarCare360.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarCare360.Api.Data;

/// <summary>
/// Контекст Entity Framework Core для базы данных CarCare360.
/// Строка подключения подаётся извне (через AddDbContext в Program.cs),
/// поэтому метод OnConfiguring здесь намеренно отсутствует.
///
/// Важные архитектурные особенности БД:
///  — Клиенты хранятся в таблице Users с RoleID = «Клиент» (отдельной таблицы Clients нет).
///  — Механик и бокс назначаются на каждую услугу (OrderServices), а не на весь заказ.
///  — PasswordHash имеет тип BINARY(64) — BCrypt-строка в ASCII-байтах.
///  — Таблицы Users, Cars, Orders имеют триггеры аудита — EF Core должен о них знать.
/// </summary>
public class CarCareDbContext : DbContext
{
    public CarCareDbContext(DbContextOptions<CarCareDbContext> options) : base(options) { }

    // ===== Пользователи и роли =====

    /// <summary>Роли: Администратор, Механик, Клиент.</summary>
    public DbSet<Role> Roles { get; set; } = null!;

    /// <summary>Все пользователи системы (сотрудники и клиенты).</summary>
    public DbSet<User> Users { get; set; } = null!;

    // ===== Автомобили =====

    /// <summary>Марки автомобилей (справочник).</summary>
    public DbSet<CarBrand> CarBrands { get; set; } = null!;

    /// <summary>Модели автомобилей (справочник).</summary>
    public DbSet<CarModel> CarModels { get; set; } = null!;

    /// <summary>Автомобили клиентов.</summary>
    public DbSet<Car> Cars { get; set; } = null!;

    // ===== Сотрудники =====

    /// <summary>Специализации механиков (справочник).</summary>
    public DbSet<Specialization> Specializations { get; set; } = null!;

    /// <summary>Профили механиков.</summary>
    public DbSet<Mechanic> Mechanics { get; set; } = null!;

    /// <summary>Боксы (посты) сервиса (справочник). В БД: ServiceBays.</summary>
    public DbSet<ServiceBay> ServiceBays { get; set; } = null!;

    // ===== Услуги =====

    /// <summary>Справочник услуг: название, нормо-часы, базовая цена. В БД: Services.</summary>
    public DbSet<Service> Services { get; set; } = null!;

    // ===== Заказы =====

    /// <summary>Заказы-наряды.</summary>
    public DbSet<Order> Orders { get; set; } = null!;

    /// <summary>Услуги в составе заказов (с привязкой механика и бокса).</summary>
    public DbSet<OrderService> OrderServices { get; set; } = null!;

    // ===== Склад =====

    /// <summary>Запчасти на складе.</summary>
    public DbSet<Part> Parts { get; set; } = null!;

    /// <summary>Запчасти в составе заказов.</summary>
    public DbSet<OrderPart> OrderParts { get; set; } = null!;

    // ===== Аудит =====

    /// <summary>Журнал аудита (заполняется триггерами SQL Server).</summary>
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    // ===== Refresh-токены (новая таблица, не входит в исходные 14) =====

    /// <summary>Refresh-токены для обновления JWT.</summary>
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    // ===== Избранные механики (новая таблица, Изменение №2, Доработка 3) =====

    /// <summary>Избранные механики клиентов (мобильное приложение).</summary>
    public DbSet<FavoriteMechanic> FavoriteMechanics { get; set; } = null!;

    /// <summary>
    /// Настройка модели: индексы, уникальные ограничения, явные FK для
    /// неоднозначных связей (несколько FK от одной сущности к User).
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Индексы для поиска ─────────────────────────────────────────────
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Login)
            .IsUnique()
            .HasDatabaseName("IX_Users_Login");

        modelBuilder.Entity<Car>()
            .HasIndex(c => c.LicensePlate)
            .HasDatabaseName("IX_Cars_LicensePlate");

        modelBuilder.Entity<Car>()
            .HasIndex(c => c.VIN)
            .HasDatabaseName("IX_Cars_VIN");

        modelBuilder.Entity<Part>()
            .HasIndex(p => p.PartNumber)
            .HasDatabaseName("IX_Parts_PartNumber");

        // ── Car → User (клиент) ────────────────────────────────────────────
        modelBuilder.Entity<Car>()
            .HasOne(c => c.Client)
            .WithMany(u => u.Cars)
            .HasForeignKey(c => c.ClientID)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Order → User (клиент) ──────────────────────────────────────────
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Client)
            .WithMany(u => u.ClientOrders)
            .HasForeignKey(o => o.ClientID)
            .OnDelete(DeleteBehavior.Restrict);

        // ── OrderService → Mechanic ───────────────────────────────────────
        //    В БД FK_OS_Mechanic ссылается на Mechanics(MechanicID), а не на Users.
        modelBuilder.Entity<OrderService>()
            .HasOne(os => os.Mechanic)
            .WithMany(m => m.AssignedServices)
            .HasForeignKey(os => os.MechanicID)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Mechanic → User (один к одному) ───────────────────────────────
        modelBuilder.Entity<Mechanic>()
            .HasOne(m => m.User)
            .WithOne(u => u.MechanicProfile)
            .HasForeignKey<Mechanic>(m => m.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        // ── AuditLog: маппинг колонок ──────────────────────────────────────
        modelBuilder.Entity<AuditLog>()
            .Property(a => a.Operation)
            .HasColumnType("char(1)");

        modelBuilder.Entity<AuditLog>()
            .Property(a => a.LogID)
            .HasColumnType("bigint");

        // ── RefreshToken: уникальный индекс по значению токена + FK на User ─
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_Token");

        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        // ── FavoriteMechanics: уникальная пара (UserID, MechanicID) + FK ───
        modelBuilder.Entity<FavoriteMechanic>()
            .HasIndex(f => new { f.UserID, f.MechanicID })
            .IsUnique()
            .HasDatabaseName("UQ_FavoriteMechanics");

        // FK на User — каскадное удаление (избранное — личные данные клиента).
        modelBuilder.Entity<FavoriteMechanic>()
            .HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        // FK на Mechanic — без каскада (механики деактивируются, не удаляются).
        modelBuilder.Entity<FavoriteMechanic>()
            .HasOne(f => f.Mechanic)
            .WithMany()
            .HasForeignKey(f => f.MechanicID)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Триггеры аудита: EF Core должен знать о них, чтобы не использовать
        //    OUTPUT clause при INSERT (иначе — ошибка «target table has database triggers»).
        //    При наличии триггеров EF Core переключается на SELECT SCOPE_IDENTITY().
        modelBuilder.Entity<User>()
            .ToTable(tb => tb.HasTrigger("trg_Audit_Users"));

        modelBuilder.Entity<Car>()
            .ToTable(tb => tb.HasTrigger("trg_Audit_Cars"));

        modelBuilder.Entity<Order>()
            .ToTable(tb => tb.HasTrigger("trg_Audit_Orders"));
    }
}
