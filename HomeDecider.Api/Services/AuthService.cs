using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HomeDecider.Api.Data;
using HomeDecider.Api.Dtos;
using HomeDecider.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace HomeDecider.Api.Services;

public class AuthService(AppDbContext db, IPasswordHasher<User> hasher, IConfiguration config, ILogger<AuthService> logger) : IAuthService
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        logger.LogInformation("Login attempt for username '{Username}'.", request.Username);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user is null)
        {
            logger.LogWarning("Login failed — username '{Username}' not found.", request.Username);
            return null;
        }

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Login failed — wrong password for username '{Username}'.", request.Username);
            return null;
        }

        logger.LogInformation("Login successful for username '{Username}'.", request.Username);
        var token = GenerateToken(user);
        return new LoginResponse(token, user.Username, user.IsAdmin, user.CanCreatePolls);
    }

    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT_SECRET"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddHours(double.Parse(config["Jwt:ExpiryHours"] ?? "72"));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim("isAdmin", user.IsAdmin.ToString().ToLower()),
            new Claim("canCreatePolls", user.CanCreatePolls.ToString().ToLower()),
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
