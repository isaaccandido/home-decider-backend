namespace HomeDecider.Api.Dtos;

public record LoginRequest(string Username, string Password);

public record LoginResponse(string Token, string Username, bool IsAdmin, bool CanCreatePolls);

public record UserDto(int Id, string Username, bool IsAdmin, bool CanCreatePolls);

public record CreateUserRequest(string Username, string Password, bool IsAdmin, bool CanCreatePolls);

public record UpdateUserRequest(bool IsAdmin, bool CanCreatePolls);
