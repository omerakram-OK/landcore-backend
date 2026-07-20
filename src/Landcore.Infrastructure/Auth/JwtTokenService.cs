using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Landcore.Application.Interfaces;
using Landcore.Infrastructure.Configuration;
using Microsoft.IdentityModel.Tokens;
using CommonClaimTypes = Landcore.Common.Constants.ClaimTypes;

namespace Landcore.Infrastructure.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(JwtSettings settings)
    {
        _settings = settings;
    }

    public JwtIssueResult GenerateToken(JwtIssueRequest request)
    {
        if (string.IsNullOrWhiteSpace(_settings.SigningKey))
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey is not configured. Set it via user-secrets or the Jwt__SigningKey " +
                "environment variable before issuing tokens — see JwtSettings.cs.");
        }

        var now = DateTime.UtcNow;
        var expiryMinutes = _settings.ExpiryMinutes > 0 ? _settings.ExpiryMinutes : 60;
        var expiresAtUtc = now.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.UserId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(CommonClaimTypes.UserId, request.UserId),
            new(CommonClaimTypes.Role, request.Role),
        };

        if (!string.IsNullOrEmpty(request.AdminId))
        {
            claims.Add(new Claim(CommonClaimTypes.AdminId, request.AdminId));
        }

        if (!string.IsNullOrEmpty(request.Email))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, request.Email));
        }

        if (!string.IsNullOrEmpty(request.FullName))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Name, request.FullName));
        }

        foreach (var permission in request.Permissions)
        {
            claims.Add(new Claim(CommonClaimTypes.Permission, permission));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new JwtIssueResult(tokenString, expiresAtUtc);
    }
}
