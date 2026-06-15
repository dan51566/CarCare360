using CarCare360.Api.Data;
using CarCare360.Api.Helpers;
using CarCare360.Api.Models.Dtos;
using CarCare360.Api.Models.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CarCare360.Api.Services;

/// <summary>
/// Реализация аутентификации.
/// Пароли проверяются через BCrypt (совместимо с десктопом).
/// Refresh-токены хранятся в таблице RefreshTokens.
/// </summary>
public class AuthService : IAuthService
{
    private readonly CarCareDbContext _db;
    private readonly ITokenService _tokens;
    private readonly LoginAttemptTracker _attempts;
    private readonly JwtSettings _jwt;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        CarCareDbContext db,
        ITokenService tokens,
        LoginAttemptTracker attempts,
        IOptions<JwtSettings> jwt,
        ILogger<AuthService> logger)
    {
        _db = db;
        _tokens = tokens;
        _attempts = attempts;
        _jwt = jwt.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Login == request.Login);

        // Не раскрываем, что именно неверно (логин или пароль)
        if (user is null)
            throw ApiException.Unauthorized("Неверный логин или пароль.");

        if (user.IsDeleted == true)
            throw ApiException.Unauthorized("Неверный логин или пароль.");

        if (user.IsActive == false)
            throw ApiException.Forbidden("Учётная запись заблокирована. Обратитесь к администратору.");

        if (!PasswordHelper.Verify(request.Password, user.PasswordHash))
        {
            var failures = _attempts.RecordFailure(request.Login);
            _logger.LogWarning("Неудачный вход: {Login} (попытка {Count})", request.Login, failures);

            // После порога неудач — блокируем учётную запись
            if (failures >= LoginAttemptTracker.MaxFailedAttempts)
            {
                user.IsActive = false;
                await _db.SaveChangesAsync();
                _attempts.Reset(request.Login);
                throw ApiException.Forbidden(
                    "Учётная запись заблокирована после нескольких неудачных попыток входа.");
            }

            throw ApiException.Unauthorized("Неверный логин или пароль.");
        }

        _attempts.Reset(request.Login);
        return await BuildAuthResponseAsync(user);
    }

    /// <inheritdoc />
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Проверка уникальности логина (дружелюбное сообщение вместо ошибки БД)
        if (await _db.Users.AnyAsync(u => u.Login == request.Login))
            throw ApiException.Conflict("Пользователь с таким логином уже существует.");

        var passwordHash = PasswordHelper.Hash(request.Password);

        // Регистрация через хранимую процедуру RegisterClient (роль «Клиент» назначается внутри SP)
        var newUserId = await _db.ExecuteScalarIntAsync(
            "RegisterClient",
            new SqlParameter("@Login", request.Login),
            new SqlParameter("@PasswordHash", System.Data.SqlDbType.Binary, 64) { Value = passwordHash },
            new SqlParameter("@FullName", request.FullName),
            new SqlParameter("@Email", (object?)request.Email ?? DBNull.Value),
            new SqlParameter("@Phone", (object?)request.Phone ?? DBNull.Value));

        var user = await _db.Users
            .Include(u => u.Role)
            .FirstAsync(u => u.UserID == newUserId);

        _logger.LogInformation("Зарегистрирован новый клиент: {Login} (ID={Id})", user.Login, user.UserID);
        return await BuildAuthResponseAsync(user);
    }

    /// <inheritdoc />
    public async Task<AuthResponse> RefreshAsync(string refreshToken)
    {
        var stored = await _db.RefreshTokens
            .Include(rt => rt.User).ThenInclude(u => u!.Role)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (stored is null || !stored.IsActive)
            throw ApiException.Unauthorized("Refresh-токен недействителен или истёк.");

        if (stored.User is null || stored.User.IsActive == false || stored.User.IsDeleted == true)
            throw ApiException.Forbidden("Учётная запись недоступна.");

        // Ротация: отзываем старый токен и выдаём новый
        stored.RevokedAt = DateTime.UtcNow;
        return await BuildAuthResponseAsync(stored.User);
    }

    /// <inheritdoc />
    public async Task LogoutAsync(string refreshToken)
    {
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
        if (stored is not null && stored.RevokedAt is null)
        {
            stored.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        // Идемпотентно: если токена нет — выход всё равно считается успешным
    }

    /// <summary>
    /// Формирует ответ: access-токен + новый refresh-токен (сохраняется в БД) + данные пользователя.
    /// </summary>
    private async Task<AuthResponse> BuildAuthResponseAsync(User user)
    {
        var (accessToken, expiresAt) = _tokens.CreateAccessToken(user);
        var refreshToken = _tokens.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserID = user.UserID,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = expiresAt,
            User = user.ToDto()
        };
    }
}
