namespace  EventUtils;

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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

    public string GenerateGuestJwtToken(string userId, string role)
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
                new Claim("CustomerId", "0"),
                new Claim("Guest","1")
            }),
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
    public string GenerateJwtToken(string userId, string role, int customerId = 0)
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
            Expires = DateTime.UtcNow.AddMinutes(60),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task<bool> ValidateJwtToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            throw new ArgumentException("Token cannot be null or empty.", nameof(token));
        // Rotate refresh token
        JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
        var result = await tokenHandler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSymmetricKey) ?? throw new ArgumentException("JWT symmetric key is not configured.")),
        });
        return result.IsValid;
    }

    public Tuple<string, string, string> GetClaimsFromToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            throw new ArgumentException("Token cannot be null or empty.", nameof(token));
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid");
        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "role");
        var customerIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "CustomerId");
        if (userIdClaim == null || roleClaim == null)
            throw new ArgumentException("Token does not contain required claims.");
        return new Tuple<string, string, string>(
            userIdClaim.Value,
            roleClaim.Value,
            customerIdClaim?.Value ?? "0");
    }

    public string GenerateRefreshToken(string userId, string role, int customerId = 0)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var refreshTokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role),
                new Claim("CustomerId", customerId.ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(240), // refresh lifetime (10 days)
            SigningCredentials = new SigningCredentials(
                //todo: use a different key for refresh tokens if needed
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSymmetricKey)),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var refreshToken = tokenHandler.CreateToken(refreshTokenDescriptor);
        string refreshTokenString = tokenHandler.WriteToken(refreshToken);
        return refreshTokenString;
    }
}