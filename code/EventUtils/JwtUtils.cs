namespace  EventUtils;

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Stripe;

public class JwtUtils
{
    private readonly string _jwtSymmetricKey;

    public JwtUtils(IConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration), "Configuration cannot be null.");
        _jwtSymmetricKey = configuration["Jwt:SymmetricKey"] ?? throw new ArgumentException("JWT symmetric key is not configured.", nameof(configuration));
        if (string.IsNullOrEmpty(_jwtSymmetricKey))
            throw new ArgumentException("JWT symmetric key is not configured.", nameof(configuration));
    }

    public string GenerateJwtToken(string userId, string role, int customerId=0)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
        if (string.IsNullOrEmpty(role))
            throw new ArgumentException("Role cannot be null or empty.", nameof(role));
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = System.Text.Encoding.ASCII.GetBytes(_jwtSymmetricKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role),
                new Claim("CustomerId", customerId.ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}