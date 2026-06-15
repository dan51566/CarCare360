using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CarCare360.Api.Helpers;
using CarCare360.Api.Models.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CarCare360.Api.Services;

/// <summary>
/// Реализация генерации JWT access-токенов и refresh-токенов.
/// </summary>
public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IOptions<JwtSettings> settings) => _settings = settings.Value;

    /// <inheritdoc />
    public (string token, DateTime expiresAt) CreateAccessToken(User user)
    {
        var roleName = user.Role?.Name ?? string.Empty;
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes);

        // Claims: идентификатор, логин, ФИО, роль
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
            new(ClaimTypes.Name, user.Login),
            new(ClaimTypes.Role, roleName),
            new("fullName", user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return (token, expiresAt);
    }

    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        // 64 случайных байта → Base64Url-строка
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
