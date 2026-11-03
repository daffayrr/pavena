using Pavena.Application.Identity.DTOs;

namespace Pavena.Application.Identity.Interfaces;

public interface IAuthenticationService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<bool> VerifyEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default);
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
}

public interface IUserProfileService
{
    Task<UserProfileDto?> GetProfileByUserIdAsync(Guid userId, Guid? callerUserId = null, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> GetProfileByUsernameAsync(string username, Guid? callerUserId = null, CancellationToken cancellationToken = default);
    Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
}

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(Guid userId, string email, string username, IEnumerable<string> roles);
    string GenerateRefreshToken();
    string HashToken(string token);
}
