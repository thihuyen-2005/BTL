using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ExamSchedule.Api.Entities;
using Microsoft.IdentityModel.Tokens;

namespace ExamSchedule.Api.Services;

public class JwtService
{
    private readonly IConfiguration _cf;

    public JwtService(IConfiguration cf)
    {
        _cf = cf;
    }

    public string GenerateAccessToken(AppUser user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(
            roles.Select(r => new Claim(ClaimTypes.Role, r))
        );

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_cf["Jwt:Key"]!)
        );

        var creds = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: _cf["Jwt:Issuer"],
            audience: _cf["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                int.Parse(_cf["Jwt:AccessTokenMinutes"]!)
            ),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
        => Convert.ToBase64String(Guid.NewGuid().ToByteArray());
}