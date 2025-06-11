using GradeBotWebAPI.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class TokenService
{
    private readonly JwtSettings _jwtSettings;

    public TokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateToken(string email, string role) //возвращает токен
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, email), //создание claims-данных, которые вкладываются в токен
            new Claim(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key)); //создание ключа и подписей
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken( //формирование токена
            issuer: _jwtSettings.Issuer, //кто выдал токен
            audience: _jwtSettings.Audience, //кто принимает токен
            claims: claims, //данные о пользователе
            expires: DateTime.Now.AddMinutes(_jwtSettings.ExpiresInMinutes), //время жизни токена
            signingCredentials: creds //секретная подпись
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
