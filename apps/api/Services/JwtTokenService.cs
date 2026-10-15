using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using api.Common.Constants;
using api.Entities;
using Microsoft.IdentityModel.Tokens;

namespace api.Services;

public class JwtTokenService(IConfiguration config)
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromHours(8);

    public string GenerateToken(User user, TimeSpan? lifetime = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        if (user.CentreId.HasValue) claims.Add(new Claim(AppClaims.CentreId, user.CentreId.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime ?? DefaultLifetime),
            signingCredentials: new SigningCredentials(SigningKey(), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Returns the user id from a valid, unexpired token, or null when the token cannot be trusted.
    public Guid? ReadUserId(string token)
    {
        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = config["Jwt:Issuer"],
                ValidAudience = config["Jwt:Audience"],
                IssuerSigningKey = SigningKey()
            }, out _);
            return Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
        }
        // A malformed token throws an ArgumentException rather than a SecurityTokenException.
        catch (Exception exception) when (exception is SecurityTokenException or ArgumentException)
        {
            return null;
        }
    }

    private SymmetricSecurityKey SigningKey() => new(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
}
