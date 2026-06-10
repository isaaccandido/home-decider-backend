using HomeDecider.Api.Data;
using HomeDecider.Api.Dtos;
using HomeDecider.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HomeDecider.Api.Services;

public class UserService(AppDbContext db, IPasswordHasher<User> hasher, ILogger<UserService> logger) : IUserService
{
    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        var users = await db.Users.OrderBy(u => u.Id).ToListAsync();
        return users.Select(u => new UserDto(u.Id, u.Username, u.IsAdmin, u.CanCreatePolls));
    }

    public async Task<(UserDto? User, string? Error)> CreateAsync(CreateUserRequest request, string actor)
    {
        if (await db.Users.AnyAsync(u => u.Username == request.Username))
            return (null, "Username already taken.");

        var user = new User
        {
            Username = request.Username.Trim(),
            IsAdmin = request.IsAdmin,
            CanCreatePolls = request.CanCreatePolls,
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        logger.LogInformation("User '{NewUser}' created by '{Actor}' (admin={IsAdmin}, canCreatePolls={CanCreate}).",
            user.Username, actor, user.IsAdmin, user.CanCreatePolls);

        return (new UserDto(user.Id, user.Username, user.IsAdmin, user.CanCreatePolls), null);
    }

    public async Task<UserDto?> UpdateAsync(int id, UpdateUserRequest request, string actor)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return null;

        user.IsAdmin = request.IsAdmin;
        user.CanCreatePolls = request.CanCreatePolls;
        await db.SaveChangesAsync();

        logger.LogInformation("User '{Username}' updated by '{Actor}' (admin={IsAdmin}, canCreatePolls={CanCreate}).",
            user.Username, actor, user.IsAdmin, user.CanCreatePolls);

        return new UserDto(user.Id, user.Username, user.IsAdmin, user.CanCreatePolls);
    }

    public async Task<bool> DeleteAsync(int id, int currentUserId, string actor)
    {
        if (id == currentUserId) return false;

        var user = await db.Users.FindAsync(id);
        if (user is null) return false;

        db.Users.Remove(user);
        await db.SaveChangesAsync();

        logger.LogInformation("User '{Username}' deleted by '{Actor}'.", user.Username, actor);
        return true;
    }
}
