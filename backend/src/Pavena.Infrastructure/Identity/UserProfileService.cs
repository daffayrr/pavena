using Microsoft.EntityFrameworkCore;
using Pavena.Application.Common.Interfaces;
using Pavena.Application.Identity.DTOs;
using Pavena.Application.Identity.Interfaces;
using Pavena.Domain.Common;
using Pavena.Domain.Enums;

namespace Pavena.Infrastructure.Identity;

public class UserProfileService : IUserProfileService
{
    private readonly IPavenaDbContext _dbContext;

    public UserProfileService(IPavenaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserProfileDto?> GetProfileByUserIdAsync(Guid userId, Guid? callerUserId = null, CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile == null)
            return null;

        if (profile.Visibility == ProfileVisibility.Private && callerUserId != userId)
        {
            return new UserProfileDto(
                profile.UserId,
                profile.DisplayName,
                null, null, null, null,
                profile.PreferredLanguage,
                profile.AvatarUrl,
                null,
                profile.Visibility.ToString(),
                profile.UpdatedAtUtc
            );
        }

        return new UserProfileDto(
            profile.UserId,
            profile.DisplayName,
            profile.Headline,
            profile.Bio,
            profile.CountryCode,
            profile.City,
            profile.PreferredLanguage,
            profile.AvatarUrl,
            profile.BannerUrl,
            profile.Visibility.ToString(),
            profile.UpdatedAtUtc
        );
    }

    public async Task<UserProfileDto?> GetProfileByUsernameAsync(string username, Guid? callerUserId = null, CancellationToken cancellationToken = default)
    {
        var normalized = username.ToUpperInvariant();
        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.NormalizedUserName == normalized, cancellationToken);

        if (user?.Profile == null)
            return null;

        return await GetProfileByUserIdAsync(user.Id, callerUserId, cancellationToken);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile == null)
            throw new DomainException("Profile not found.");

        if (!Enum.TryParse<ProfileVisibility>(request.Visibility, true, out var visibility))
            visibility = ProfileVisibility.Public;

        profile.DisplayName = request.DisplayName;
        profile.Headline = request.Headline;
        profile.Bio = request.Bio;
        profile.CountryCode = request.CountryCode;
        profile.City = request.City;
        profile.PreferredLanguage = request.PreferredLanguage;
        profile.Visibility = visibility;
        profile.UpdatedAtUtc = DateTime.UtcNow;
        profile.ConcurrencyStamp = Guid.NewGuid().ToString();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UserProfileDto(
            profile.UserId,
            profile.DisplayName,
            profile.Headline,
            profile.Bio,
            profile.CountryCode,
            profile.City,
            profile.PreferredLanguage,
            profile.AvatarUrl,
            profile.BannerUrl,
            profile.Visibility.ToString(),
            profile.UpdatedAtUtc
        );
    }
}
