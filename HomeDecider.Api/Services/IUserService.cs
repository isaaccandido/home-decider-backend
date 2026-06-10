using HomeDecider.Api.Dtos;

namespace HomeDecider.Api.Services;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllAsync();
    Task<(UserDto? User, string? Error)> CreateAsync(CreateUserRequest request, string actor);
    Task<UserDto?> UpdateAsync(int id, UpdateUserRequest request, string actor);
    Task<bool> DeleteAsync(int id, int currentUserId, string actor);
}
