using CarCare360.Desktop.Data;
using CarCare360.Desktop.Helpers;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CarCare360.Desktop.ViewModels;

public record StatusStatDto(string Status, int Count);

/// <summary>DTO статистики нагрузки механика.</summary>
public record MechanicStatDto(
    string  FullName,
    int     OrdersCount,
    decimal ServicesTotal,
    double  AvgDays,
    int     ActiveOrders
);

/// <summary>
/// ViewModel раздела «Отчёты».
/// Два таба: Финансы (KPI-дашборд) + Механики (нагрузка).
/// Excel экспорт — заглушка.
/// </summary>
public sealed class ReportsViewModel : BaseViewModel
{
    public static readonly string[] PeriodOptions =
        ["Сегодня", "7 дней", "30 дней", "Этот месяц", "Всё время"];

    private string  _selectedPeriod     = "30 дней";
    private int     _selectedReportTab;   // 0 = Финансы, 1 = Механики
    private bool    _isLoading;

    // KPI
    private int     _totalOrders;
    private decimal _totalRevenue;
    private int     _newClientsCount;
    private int     _activeMechanicsCount;
    private int     _lowStockCount;
    private decimal _warehouseTotalValue;

    public ReportsViewModel()
    {
        RefreshCommand      = new RelayCommand(async () => await LoadAsync());
        SelectTabCommand    = new RelayCommand<string>(s =>
        {
            if (int.TryParse(s, out var idx))
                SelectedReportTab = idx;
        });
        ExportExcelCommand  = new RelayCommand(() =>
            ToastHelper.Show(
                "Экспорт пока не реализован. Функция будет добавлена в следующей версии.",
                ToastType.Info));
    }

    // ── Свойства ─────────────────────────────────────────────────────────

    public string SelectedPeriod
    {
        get => _selectedPeriod;
        set { if (SetProperty(ref _selectedPeriod, value)) _ = LoadAsync(); }
    }

    public int SelectedReportTab
    {
        get => _selectedReportTab;
        set { if (SetProperty(ref _selectedReportTab, value)) _ = LoadAsync(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    // ── KPI ──────────────────────────────────────────────────────────────

    public int TotalOrders
    {
        get => _totalOrders;
        private set => SetProperty(ref _totalOrders, value);
    }

    public decimal TotalRevenue
    {
        get => _totalRevenue;
        private set => SetProperty(ref _totalRevenue, value);
    }

    public int NewClientsCount
    {
        get => _newClientsCount;
        private set => SetProperty(ref _newClientsCount, value);
    }

    public int ActiveMechanicsCount
    {
        get => _activeMechanicsCount;
        private set => SetProperty(ref _activeMechanicsCount, value);
    }

    public int LowStockCount
    {
        get => _lowStockCount;
        private set => SetProperty(ref _lowStockCount, value);
    }

    public decimal WarehouseTotalValue
    {
        get => _warehouseTotalValue;
        private set => SetProperty(ref _warehouseTotalValue, value);
    }

    public ObservableCollection<StatusStatDto>  OrdersByStatus { get; } = new();
    public ObservableCollection<MechanicStatDto> MechanicStats  { get; } = new();

    // ── Баг 5: Графики (Finance-вкладка) ────────────────────────────────

    /// <summary>Столбчатый график выручки по дням (Услуги + Запчасти).</summary>
    private ISeries[] _revenueSeries = [];
    public ISeries[] RevenueSeries
    {
        get => _revenueSeries;
        private set => SetProperty(ref _revenueSeries, value);
    }

    /// <summary>Оси X столбчатого графика (метки дат).</summary>
    private Axis[] _revenueXAxes = [new Axis { Labels = Array.Empty<string>() }];
    public Axis[] RevenueXAxes
    {
        get => _revenueXAxes;
        private set => SetProperty(ref _revenueXAxes, value);
    }

    /// <summary>Donut-диаграмма структуры доходов: Услуги vs Запчасти.</summary>
    private ISeries[] _structureSeries = [];
    public ISeries[] StructureSeries
    {
        get => _structureSeries;
        private set => SetProperty(ref _structureSeries, value);
    }

    /// <summary>Линейный график среднего чека по дням.</summary>
    private ISeries[] _avgCheckSeries = [];
    public ISeries[] AvgCheckSeries
    {
        get => _avgCheckSeries;
        private set => SetProperty(ref _avgCheckSeries, value);
    }

    // ── Команды ──────────────────────────────────────────────────────────

    public ICommand RefreshCommand     { get; }
    public ICommand SelectTabCommand   { get; }
    public ICommand ExportExcelCommand { get; }

    // ── Загрузка ─────────────────────────────────────────────────────────

    private (DateTime from, DateTime to) GetPeriod()
    {
        var now = DateTime.Now;
        return SelectedPeriod switch
        {
            "Сегодня"     => (now.Date, now.Date.AddDays(1)),
            "7 дней"      => (now.Date.AddDays(-7), now.Date.AddDays(1)),
            "Этот месяц"  => (new DateTime(now.Year, now.Month, 1), now.Date.AddDays(1)),
            "Всё время"   => (DateTime.MinValue, DateTime.MaxValue),
            _             => (now.Date.AddDays(-30), now.Date.AddDays(1))
        };
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            if (SelectedReportTab == 0)
                await LoadFinanceAsync();
            else
                await LoadMechanicStatsAsync();
        }
        catch { /* показываем нули */ }
        finally { IsLoading = false; }
    }

    private async Task LoadFinanceAsync()
    {
        var (from, to) = GetPeriod();
        await using var db = new CarCareDbContext();

        TotalOrders = await db.Orders
            .Where(o => o.IsDeleted != true && o.CreatedAt >= from && o.CreatedAt < to)
            .CountAsync();

        var serviceRevenue = await db.OrderServices
            .Include(os => os.Service)
            .Include(os => os.Order)
            .Where(os => os.Order!.IsDeleted != true &&
                         os.Order.CreatedAt >= from &&
                         os.Order.CreatedAt < to)
            .SumAsync(os => (decimal?)(os.Service != null ? os.Service.BasePrice ?? 0m : 0m)) ?? 0m;

        var partRevenue = await db.OrderParts
            .Include(op => op.Order)
            .Where(op => op.Order!.IsDeleted != true &&
                         op.Order.CreatedAt >= from &&
                         op.Order.CreatedAt < to)
            .SumAsync(op => (decimal?)((op.PricePerUnit ?? 0m) * op.Quantity)) ?? 0m;

        TotalRevenue = serviceRevenue + partRevenue;

        // Баг 5 — загружаем временной ряд для графиков.
        // Группируем заказы по дате создания: отдельно выручка от услуг и запчастей.
        var dailyRevenue = await db.Orders
            .Where(o => o.IsDeleted != true && o.CreatedAt >= from && o.CreatedAt < to)
            .Select(o => new
            {
                Date     = o.CreatedAt!.Value.Date,
                Services = o.OrderServices!.Sum(os => (decimal?)(os.Service != null ? os.Service.BasePrice ?? 0m : 0m)) ?? 0m,
                Parts    = o.OrderParts!.Sum(op => (decimal?)((op.PricePerUnit ?? 0m) * op.Quantity)) ?? 0m
            })
            .ToListAsync();

        // Группируем по дате (один заказ — одна строка с агрегатами)
        var byDay = dailyRevenue
            .GroupBy(x => x.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Date     = g.Key,
                Services = g.Sum(x => x.Services),
                Parts    = g.Sum(x => x.Parts)
            })
            .ToList();

        var labels = byDay.Select(d => d.Date.ToString("dd.MM")).ToArray();

        // Столбчатый график: Услуги (синие) + Запчасти (оранжевые)
        RevenueSeries =
        [
            new ColumnSeries<double>
            {
                Name   = "Услуги",
                Values = byDay.Select(d => (double)d.Services).ToArray(),
                Fill   = new SolidColorPaint(SKColor.Parse("#1A237E")),
                Stroke = null
            },
            new ColumnSeries<double>
            {
                Name   = "Запчасти",
                Values = byDay.Select(d => (double)d.Parts).ToArray(),
                Fill   = new SolidColorPaint(SKColor.Parse("#FF6B00")),
                Stroke = null
            }
        ];
        RevenueXAxes = [new Axis { Labels = labels, LabelsRotation = -35, TextSize = 11 }];

        // Donut-диаграмма структуры доходов.
        // Values принимает ICollection<double> — передаём new double[].
        StructureSeries =
        [
            new PieSeries<double>
            {
                Name            = "Услуги",
                Values          = new double[] { (double)serviceRevenue },
                Fill            = new SolidColorPaint(SKColor.Parse("#1A237E")),
                InnerRadius     = 60,
                OuterRadiusOffset = 10
            },
            new PieSeries<double>
            {
                Name            = "Запчасти",
                Values          = new double[] { (double)partRevenue },
                Fill            = new SolidColorPaint(SKColor.Parse("#FF6B00")),
                InnerRadius     = 60,
                OuterRadiusOffset = 10
            }
        ];

        // Линейный график среднего чека
        AvgCheckSeries =
        [
            new LineSeries<double>
            {
                Name         = "Средний чек",
                Values       = byDay.Select(d =>
                {
                    var total = (double)(d.Services + d.Parts);
                    return total > 0 ? total : 0;
                }).ToArray(),
                Fill         = null,
                Stroke       = new SolidColorPaint(SKColor.Parse("#1A237E"), 2),
                GeometrySize = 8,
                GeometryFill = new SolidColorPaint(SKColor.Parse("#FF6B00")),
                GeometryStroke = new SolidColorPaint(SKColor.Parse("#1A237E"), 1)
            }
        ];

        NewClientsCount = await db.Users
            .Include(u => u.Role)
            .Where(u => u.Role!.Name == "Клиент" &&
                        u.IsDeleted != true &&
                        u.CreatedAt >= from &&
                        u.CreatedAt < to)
            .CountAsync();

        var periodServiceIds = db.OrderServices
            .Include(os => os.Order)
            .Where(os => os.Order!.CreatedAt >= from && os.Order.CreatedAt < to && os.MechanicID != null)
            .Select(os => os.MechanicID!.Value)
            .Distinct();
        ActiveMechanicsCount = await periodServiceIds.CountAsync();

        LowStockCount = await db.Parts.Where(p => p.QuantityInStock <= 5).CountAsync();

        WarehouseTotalValue = await db.Parts
            .SumAsync(p => (decimal?)((p.Price ?? 0m) * (p.QuantityInStock ?? 0))) ?? 0m;

        var statuses = await db.Orders
            .Where(o => o.IsDeleted != true && o.CreatedAt >= from && o.CreatedAt < to)
            .GroupBy(o => o.Status ?? "Новый")
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        OrdersByStatus.Clear();
        foreach (var s in statuses)
            OrdersByStatus.Add(new StatusStatDto(s.Status, s.Count));
    }

    private async Task LoadMechanicStatsAsync()
    {
        var (from, to) = GetPeriod();
        await using var db = new CarCareDbContext();

        // Механики с навигацией на User
        var mechanics = await db.Mechanics
            .Include(m => m.User)
            .Where(m => m.User != null)
            .ToListAsync();

        // Все OrderServices в периоде с навигациями
        var orderServices = await db.OrderServices
            .Include(os => os.Order)
            .Include(os => os.Service)
            .Where(os => os.MechanicID != null &&
                         os.Order!.IsDeleted != true &&
                         os.Order.CreatedAt >= from &&
                         os.Order.CreatedAt < to)
            .ToListAsync();

        var stats = mechanics
            .Select(m =>
            {
                // Баг 3 (продолжение) — было m.UserID, но FK OrderServices.MechanicID
                // ссылается на Mechanics.MechanicID, а не на Users.UserID. Сопоставляем по MechanicID,
                // иначе нагрузка механиков считается неверно (нули или чужие заказы).
                var mServices = orderServices.Where(os => os.MechanicID == m.MechanicID).ToList();
                if (!mServices.Any()) return null;

                var ordersCount   = mServices.Select(os => os.OrderID).Distinct().Count();
                var servicesTotal = mServices.Sum(os => os.Service?.BasePrice ?? 0m);

                var orders = mServices
                    .Select(os => os.Order!)
                    .GroupBy(o => o.OrderID).Select(g => g.First())
                    .ToList();

                var avgDays = orders
                    .Where(o => o.ScheduledDate.HasValue && o.CreatedAt.HasValue)
                    .Select(o => (o.ScheduledDate!.Value - o.CreatedAt!.Value).TotalDays)
                    .DefaultIfEmpty(0)
                    .Average();

                var activeOrders = orders.Count(o => o.Status != "Выдан" && o.Status != "Отменён");

                return new MechanicStatDto(
                    m.User!.FullName,
                    ordersCount,
                    servicesTotal,
                    Math.Round(Math.Max(0, avgDays), 1),
                    activeOrders);
            })
            .OfType<MechanicStatDto>()
            .OrderByDescending(s => s.OrdersCount)
            .ToList();

        MechanicStats.Clear();
        foreach (var s in stats)
            MechanicStats.Add(s);
    }
}
