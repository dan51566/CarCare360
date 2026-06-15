namespace CarCare360.Api.Helpers;

/// <summary>
/// Прикладное исключение с конкретным HTTP-статусом.
/// Перехватывается глобальным middleware и превращается в JSON { error }.
/// Используется сервисами для возврата 400/403/404/409 без зависимости от MVC.
/// </summary>
public class ApiException : Exception
{
    /// <summary>HTTP-статус ответа.</summary>
    public int StatusCode { get; }

    public ApiException(int statusCode, string message) : base(message)
        => StatusCode = statusCode;

    public static ApiException BadRequest(string message) => new(StatusCodes.Status400BadRequest, message);
    public static ApiException Unauthorized(string message) => new(StatusCodes.Status401Unauthorized, message);
    public static ApiException Forbidden(string message) => new(StatusCodes.Status403Forbidden, message);
    public static ApiException NotFound(string message) => new(StatusCodes.Status404NotFound, message);
    public static ApiException Conflict(string message) => new(StatusCodes.Status409Conflict, message);
}
