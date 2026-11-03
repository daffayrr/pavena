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

    Task<AcademicProfileDto?> GetAcademicProfileAsync(string identifier, Guid? callerUserId = null, CancellationToken cancellationToken = default);
    Task<AcademicProfileDto?> GetAcademicProfileByUserIdAsync(Guid userId, Guid? callerUserId = null, CancellationToken cancellationToken = default);
    Task<UserEducationDto> AddEducationAsync(Guid userId, CreateEducationRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteEducationAsync(Guid userId, Guid educationId, CancellationToken cancellationToken = default);
    Task<UserExperienceDto> AddExperienceAsync(Guid userId, CreateExperienceRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteExperienceAsync(Guid userId, Guid experienceId, CancellationToken cancellationToken = default);
    Task<UserSkillDto> AddSkillAsync(Guid userId, string skillName, CancellationToken cancellationToken = default);
    Task<bool> DeleteSkillAsync(Guid userId, Guid skillId, CancellationToken cancellationToken = default);
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
