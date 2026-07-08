using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Auth;

public class JwtTokenGenerator(IOptions<JwtSettings> options)
{
    public string GenerateToken(User user, IEnumerable<string> permissions, Guid sessionId)
    {
        var settings = options.Value;
        var claims = BuildBaseClaims(user);
        claims.Add(new Claim("session_id", sessionId.ToString()));

        foreach (var permission in permissions.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim("permission", permission));
        }

        return WriteToken(claims, DateTime.UtcNow.AddHours(settings.ExpiresHours));
    }

    public string GenerateMfaChallengeToken(User user)
    {
        var claims = BuildBaseClaims(user);
        claims.Add(new Claim("mfa_challenge", "true"));

        return WriteToken(claims, DateTime.UtcNow.AddMinutes(5));
    }

    private List<Claim> BuildBaseClaims(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString()),
        };

        if (user.ProfessionalId.HasValue)
        {
            claims.Add(new Claim("professional_id", user.ProfessionalId.Value.ToString()));
        }

        if (user.PatientId.HasValue)
        {
            claims.Add(new Claim("patient_id", user.PatientId.Value.ToString()));
        }

        return claims;
    }

    private string WriteToken(IEnumerable<Claim> claims, DateTime expires)
    {
        var settings = options.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
