using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using CarCare360.Api.Data;
using CarCare360.Api.Helpers;
using CarCare360.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────────────────────
//  Конфигурация
// ─────────────────────────────────────────────────────────────────────────────

// Настройки JWT из секции "Jwt"
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                  ?? throw new InvalidOperationException("Секция конфигурации 'Jwt' не найдена.");

// ─────────────────────────────────────────────────────────────────────────────
//  База данных (EF Core, та же БД CarCare360)
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<CarCareDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CarCareDb")));

// ─────────────────────────────────────────────────────────────────────────────
//  Аутентификация и авторизация (JWT Bearer + RBAC)
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            // Имена claim'ов совпадают с теми, что выдаёт TokenService
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

// ─────────────────────────────────────────────────────────────────────────────
//  Регистрация сервисов приложения (DI)
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<LoginAttemptTracker>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<ICarService, CarService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IMechanicService, MechanicService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// ─────────────────────────────────────────────────────────────────────────────
//  CORS — для разработки разрешаем все источники (в production ограничить!)
// ─────────────────────────────────────────────────────────────────────────────
const string CorsPolicy = "AllowAll";
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ─────────────────────────────────────────────────────────────────────────────
//  Rate limiting — ограничение попыток на эндпоинтах аутентификации
//  (3 запроса в минуту с одного IP; превышение → 429)
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// ─────────────────────────────────────────────────────────────────────────────
//  Контроллеры и Swagger
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CarCare360 API",
        Version = "v1",
        Description = "Серверный API автосервиса CarCare360 (десктоп + мобильное приложение)."
    });

    // Подключаем XML-комментарии для отображения описаний в Swagger
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);

    // Кнопка Authorize: ввод JWT-токена
    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите JWT access-токен (без слова Bearer).",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", jwtScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { [jwtScheme] = Array.Empty<string>() });
});

var app = builder.Build();

// ─────────────────────────────────────────────────────────────────────────────
//  Конвейер обработки запросов
// ─────────────────────────────────────────────────────────────────────────────

// Глобальный обработчик исключений — должен быть в начале конвейера
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CarCare360 API v1"));
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Сидер тестового клиента отключён — данные очищены вручную (2026-05-29).
// Раскомментировать только для первоначальной настройки нового окружения.
// if (app.Environment.IsDevelopment())
// {
//     using var scope = app.Services.CreateScope();
//     var db = scope.ServiceProvider.GetRequiredService<CarCareDbContext>();
//     var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
//     try { await ApiSeeder.SeedAsync(db, logger); }
//     catch (Exception ex) { logger.LogError(ex, "Ошибка засева тестовых данных."); }
// }

app.Run();
