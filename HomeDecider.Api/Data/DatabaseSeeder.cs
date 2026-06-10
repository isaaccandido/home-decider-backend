using HomeDecider.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HomeDecider.Api.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        logger.LogInformation("Applying database migrations...");
        await db.Database.MigrateAsync();
        logger.LogInformation("Database ready.");

        if (!await db.Users.AnyAsync())
        {
            logger.LogInformation("No users found — seeding default admin.");
            var username = config["DEFAULT_ADMIN_USERNAME"] ?? throw new InvalidOperationException("DEFAULT_ADMIN_USERNAME is not set.");
            var password = config["DEFAULT_ADMIN_PASSWORD"] ?? throw new InvalidOperationException("DEFAULT_ADMIN_PASSWORD is not set.");

            var admin = new User { Username = username, IsAdmin = true, CanCreatePolls = true };
            admin.PasswordHash = hasher.HashPassword(admin, password);
            db.Users.Add(admin);
            await db.SaveChangesAsync();
            logger.LogInformation("Admin user '{Username}' created.", username);
        }
    }
}
