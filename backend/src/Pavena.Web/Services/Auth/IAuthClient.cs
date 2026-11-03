using Pavena.Application.Identity.DTOs;

namespace Pavena.Web.Services.Auth;

public interface IAuthClient
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<RegisterResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
}
