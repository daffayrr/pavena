namespace Pavena.Application.Identity.DTOs;

public record RegisterRequest(
    string Email,
    string Username,
    string Password,
    string DisplayName,
    string? CountryCode = null,
    string? City = null,
    string PreferredLanguage = "en"
);

public record RegisterResponse(
    Guid UserId,
    string Email,
    string Username,
    string DisplayName,
    bool RequiresEmailConfirmation
);

public record LoginRequest(
    string EmailOrUsername,
    string Password
);

public record LoginResponse(
    Guid UserId,
    string Email,
    string Username,
    string DisplayName,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAtUtc,
    IReadOnlyList<string> Roles
);

public record RefreshTokenRequest(
    string RefreshToken
);

public record ForgotPasswordRequest(
    string Email
);

public record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record UserProfileDto(
    Guid UserId,
    string DisplayName,
    string? Headline,
    string? Bio,
    string? CountryCode,
    string? City,
    string PreferredLanguage,
    string? AvatarUrl,
    string? BannerUrl,
    string Visibility,
    DateTime UpdatedAtUtc
);

public record UpdateProfileRequest(
    string DisplayName,
    string? Headline,
    string? Bio,
    string? CountryCode,
    string? City,
    string PreferredLanguage,
    string Visibility
);

public record UserSummaryDto(
    Guid UserId,
    string Email,
    string Username,
    string DisplayName,
    string Status,
    bool EmailConfirmed,
    IReadOnlyList<string> Roles,
    DateTime CreatedAtUtc
);
