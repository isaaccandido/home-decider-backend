using HomeDecider.Api.Dtos;

namespace HomeDecider.Api.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
}
