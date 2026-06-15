using System.Text.Json;

namespace CarCare360.Api.Helpers;

/// <summary>
/// Глобальный обработчик необработанных исключений.
/// Любое исключение, не пойманное в контроллере/сервисе, превращается
/// в единый JSON-ответ { error, detail } и логируется.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException apiEx)
        {
            // Ожидаемые прикладные ошибки (400/403/404/409) — без стека в лог
            _logger.LogWarning("Прикладная ошибка {Status}: {Message}", apiEx.StatusCode, apiEx.Message);
            await WriteJsonAsync(context, apiEx.StatusCode, apiEx.Message, detail: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Необработанное исключение при обработке {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteJsonAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Внутренняя ошибка сервера.",
                // Детали исключения раскрываем только в среде разработки
                detail: _env.IsDevelopment() ? ex.Message : null);
        }
    }

    private static async Task WriteJsonAsync(HttpContext context, int statusCode, string error, string? detail)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error, detail }));
    }
}
