using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pavena.Application.Common.Interfaces;
using Pavena.Application.Identity.DTOs;
using Pavena.Application.Identity.Interfaces;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Entities.Profiles;
using Pavena.Domain.Enums;

namespace Pavena.Infrastructure.Identity;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IPavenaDbContext _dbContext;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public AuthenticationService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IPavenaDbContext dbContext,
        IJwtTokenGenerator tokenGenerator)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existingEmail = await _userManager.FindByEmailAsync(request.Email);
        if (existingEmail != null)
            throw new DomainException("An account with this email address already exists.");

        var existingUsername = await _userManager.FindByNameAsync(request.Username);
        if (existingUsername != null)
            throw new DomainException("This username is already taken.");

        var user = new User(request.Email, request.Username)
        {
            Status = UserStatus.Active,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new DomainException($"Registration failed: {errors}");
        }

        var profile = new UserProfile(user.Id, request.DisplayName)
        {
            CountryCode = request.CountryCode,
            City = request.City,
            PreferredLanguage = request.PreferredLanguage ?? "en",
            Visibility = ProfileVisibility.Public,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _dbContext.UserProfiles.Add(profile);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RegisterResponse(
            user.Id,
            user.Email!,
            user.UserName!,
            profile.DisplayName,
            RequiresEmailConfirmation: !user.EmailConfirmed
        );
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.EmailOrUsername)
                   ?? await _userManager.FindByNameAsync(request.EmailOrUsername);

        if (user == null)
            throw new DomainException("Invalid credentials.");

        if (user.Status == UserStatus.Suspended)
            throw new DomainException("This account is currently suspended. Please contact platform administration.");

        if (user.Status == UserStatus.Deactivated)
            throw new DomainException("This account has been deactivated.");

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
            throw new DomainException("Account is locked due to multiple failed login attempts. Please try again in 15 minutes.");

        if (!signInResult.Succeeded)
            throw new DomainException("Invalid credentials.");

        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, expiresAtUtc) = _tokenGenerator.GenerateAccessToken(user.Id, user.Email!, user.UserName!, roles);

        var rawRefreshToken = _tokenGenerator.GenerateRefreshToken();
        var refreshTokenHash = _tokenGenerator.HashToken(rawRefreshToken);

        var refreshTokenEntity = new UserRefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _dbContext.UserRefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var profile = await _dbContext.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);
        var displayName = profile?.DisplayName ?? user.UserName!;

        return new LoginResponse(
            user.Id,
            user.Email!,
            user.UserName!,
            displayName,
            accessToken,
            rawRefreshToken,
            expiresAtUtc,
            roles.ToList()
        );
    }

    public async Task<LoginResponse> RefreshTokenAsync(string rawRefreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = _tokenGenerator.HashToken(rawRefreshToken);

        var tokenEntity = await _dbContext.UserRefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (tokenEntity == null || !tokenEntity.IsActive || tokenEntity.User == null)
            throw new DomainException("Invalid or expired refresh token.");

        var user = tokenEntity.User;

        if (user.Status != UserStatus.Active)
            throw new DomainException("Account is not active.");

        // Rotate token
        var newRawRefreshToken = _tokenGenerator.GenerateRefreshToken();
        var newRefreshTokenHash = _tokenGenerator.HashToken(newRawRefreshToken);

        tokenEntity.Revoke(newRefreshTokenHash);

        var newRefreshTokenEntity = new UserRefreshToken
        {
            UserId = user.Id,
            TokenHash = newRefreshTokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _dbContext.UserRefreshTokens.Add(newRefreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, expiresAtUtc) = _tokenGenerator.GenerateAccessToken(user.Id, user.Email!, user.UserName!, roles);

        var profile = await _dbContext.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);
        var displayName = profile?.DisplayName ?? user.UserName!;

        return new LoginResponse(
            user.Id,
            user.Email!,
            user.UserName!,
            displayName,
            accessToken,
            newRawRefreshToken,
            expiresAtUtc,
            roles.ToList()
        );
    }

    public async Task RevokeTokenAsync(string rawRefreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = _tokenGenerator.HashToken(rawRefreshToken);
        var tokenEntity = await _dbContext.UserRefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (tokenEntity != null && !tokenEntity.IsRevoked)
        {
            tokenEntity.Revoke();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> VerifyEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return false;

        var result = await _userManager.ConfirmEmailAsync(user, token);
        return result.Succeeded;
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.EmailConfirmed)
        {
            // Do not reveal whether user exists
            return;
        }

        // Generate reset token (in real app, would send via email)
        _ = await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return false;

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (result.Succeeded)
        {
            await _userManager.UpdateSecurityStampAsync(user);
            return true;
        }

        return false;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return false;

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (result.Succeeded)
        {
            await _userManager.UpdateSecurityStampAsync(user);
            return true;
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        throw new DomainException($"Change password failed: {errors}");
    }
}
