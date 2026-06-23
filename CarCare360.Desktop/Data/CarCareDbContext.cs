using CarCare360.Desktop.Models;
using Microsoft.EntityFrameworkCore;
using System.Configuration;

namespace CarCare360.Desktop.Data;

/// <summary>
/// Контекст Entity Framework Core для базы данных CarCare360.
/// Строка подключения читается из App.config (ключ "CarCareDbContext").
///
/// Важные архитектурные особенности БД:
///  — Клиенты хранятся в таблице Users с RoleID = "Клиент" (отдельная таблица Clients пуста).
///  — Механик и бокс назначаются на каждую услугу (OrderServices), а не на весь заказ.
///  — PasswordHash имеет тип BINARY(64) — BCrypt-строка в ASCII-байтах.
/// </summary>
public class CarCareDbContext : DbContext
{
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

    /// <summary>Журнал аудита входов (Изменение №2, Доработка 4).</summary>
    public DbSet<LoginAuditLog> LoginAuditLogs { get; set; } = null!;

    /// <summary>
    /// Настройка подключения к SQL Server через строку из App.config.
    /// </summary>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var cs = ConfigurationManager.ConnectionStrings["CarCareDbContext"]?.ConnectionString
                     ?? throw new InvalidOperationException(
                         "Строка подключения 'CarCareDbContext' не найдена в App.config.");

            optionsBuilder.UseSqlServer(cs);
        }
    }

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

        // ── Car → User (клиент), несколько FK от Orders/Cars к User ─────────
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

        // ── OrderService → User (механик) ─────────────────────────────────
        modelBuilder.Entity<OrderService>()
            .HasOne(os => os.Mechanic)
            .WithMany(u => u.AssignedServices)
            .HasForeignKey(os => os.MechanicID)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Mechanic → User (один к одному) ───────────────────────────────
        modelBuilder.Entity<Mechanic>()
            .HasOne(m => m.User)
            .WithOne(u => u.MechanicProfile)
            .HasForeignKey<Mechanic>(m => m.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        // ── AuditLog: маппинг колонки Operation (CHAR(1) в БД) ────────────
        modelBuilder.Entity<AuditLog>()
            .Property(a => a.Operation)
            .HasColumnType("char(1)");

        // ── Таблица AuditLog имеет PK bigint ──────────────────────────────
        modelBuilder.Entity<AuditLog>()
            .Property(a => a.LogID)
            .HasColumnType("bigint");

        // ── LoginAuditLog: Result CHAR(1) + PK bigint (Доработка 4) ────────
        modelBuilder.Entity<LoginAuditLog>()
            .Property(l => l.Result)
            .HasColumnType("char(1)");
        modelBuilder.Entity<LoginAuditLog>()
            .Property(l => l.LogID)
            .HasColumnType("bigint");

        // ── Триггеры аудита: EF Core должен знать о них, чтобы не использовать
        //    OUTPUT clause при INSERT (иначе — ошибка "target table has database triggers").
        //    При наличии триггеров EF Core переключается на SELECT SCOPE_IDENTITY().
        modelBuilder.Entity<User>()
            .ToTable(tb => tb.HasTrigger("trg_Audit_Users"));

        modelBuilder.Entity<Car>()
            .ToTable(tb => tb.HasTrigger("trg_Audit_Cars"));

        modelBuilder.Entity<Order>()
            .ToTable(tb => tb.HasTrigger("trg_Audit_Orders"));
    }
}
