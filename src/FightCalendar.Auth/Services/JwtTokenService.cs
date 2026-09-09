using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FightCalendar.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace FightCalendar.Auth.Services;

// Mints the token the frontend attaches to API requests. Any service that
// knows SigningKey can verify one of these independently - no shared
// session store, no call back to this service to ask "is this still
// valid?" (see JwtOptions for why that matters across separate services).
public class JwtTokenService(JwtOptions options)
{
    public (string Token, DateTimeOffset ExpiresAt) CreateToken(IdentityUser user)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(options.LifetimeDays);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
