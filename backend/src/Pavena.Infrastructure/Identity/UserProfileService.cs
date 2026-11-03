using Microsoft.EntityFrameworkCore;
using Pavena.Application.Common.Interfaces;
using Pavena.Application.Identity.DTOs;
using Pavena.Application.Identity.Interfaces;
using Pavena.Application.Social.DTOs;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Profiles;
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

    public async Task<AcademicProfileDto?> GetAcademicProfileAsync(string identifier, Guid? callerUserId = null, CancellationToken cancellationToken = default)
    {
        if (Guid.TryParse(identifier, out var parsedGuid))
        {
            return await GetAcademicProfileByUserIdAsync(parsedGuid, callerUserId, cancellationToken);
        }

        var normalized = identifier.ToUpperInvariant();
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.NormalizedUserName == normalized, cancellationToken);

        if (user == null)
            return null;

        return await GetAcademicProfileByUserIdAsync(user.Id, callerUserId, cancellationToken);
    }

    public async Task<AcademicProfileDto?> GetAcademicProfileByUserIdAsync(Guid userId, Guid? callerUserId = null, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.Profile)
            .Include(u => u.Educations)
            .Include(u => u.Experiences)
            .Include(u => u.Skills)
            .Include(u => u.Interests)
            .Include(u => u.Memberships)
                .ThenInclude(m => m.Organization)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null || user.Profile == null)
            return null;

        var profile = user.Profile;
        var isOwner = callerUserId == userId;

        // Relation & social metrics
        var publicationsCount = await _dbContext.Posts
            .CountAsync(p => p.AuthorUserId == userId && p.Status == PostStatus.Published && !p.IsDeleted, cancellationToken);

        var connectionsCount = await _dbContext.Connections
            .CountAsync(c => (c.RequesterUserId == userId || c.AddresseeUserId == userId) && c.Status == ConnectionStatus.Accepted, cancellationToken);

        var followersCount = await _dbContext.Follows
            .CountAsync(f => f.TargetType == FollowTargetType.User && f.TargetId == userId, cancellationToken);

        var followingCount = await _dbContext.Follows
            .CountAsync(f => f.FollowerUserId == userId && f.TargetType == FollowTargetType.User, cancellationToken);

        // Caller relation to target user
        string connectionStatus = "None";
        bool isFollowing = false;

        if (callerUserId.HasValue && !isOwner)
        {
            var callerId = callerUserId.Value;
            var connection = await _dbContext.Connections
                .AsNoTracking()
                .FirstOrDefaultAsync(c => (c.RequesterUserId == callerId && c.AddresseeUserId == userId) ||
                                          (c.RequesterUserId == userId && c.AddresseeUserId == callerId), cancellationToken);

            if (connection != null)
            {
                if (connection.Status == ConnectionStatus.Accepted)
                    connectionStatus = "Accepted";
                else if (connection.Status == ConnectionStatus.Pending)
                    connectionStatus = connection.RequesterUserId == callerId ? "PendingOutgoing" : "PendingIncoming";
                else if (connection.Status == ConnectionStatus.Rejected)
                    connectionStatus = "Rejected";
            }

            isFollowing = await _dbContext.Follows
                .AnyAsync(f => f.FollowerUserId == callerId && f.TargetType == FollowTargetType.User && f.TargetId == userId, cancellationToken);
        }

        // Completeness calculation
        var score = 0;
        if (!string.IsNullOrWhiteSpace(profile.DisplayName)) score += 10;
        if (!string.IsNullOrWhiteSpace(profile.Headline)) score += 5;
        if (!string.IsNullOrWhiteSpace(profile.Bio)) score += 5;
        if (!string.IsNullOrWhiteSpace(profile.CountryCode) || !string.IsNullOrWhiteSpace(profile.City)) score += 5;
        if (!string.IsNullOrWhiteSpace(profile.AvatarUrl)) score += 5;
        if (user.Educations.Any()) score += 20;
        if (user.Experiences.Any()) score += 20;
        if (user.Skills.Any() || user.Interests.Any()) score += 15;
        if (user.Memberships.Any()) score += 15;
        var completenessScore = Math.Min(100, score);

        var isVerified = user.Memberships.Any(m => m.Organization != null && m.Organization.VerificationStatus == OrganizationVerificationStatus.Verified);

        // Privacy check
        var isRestricted = false;
        if (profile.Visibility == ProfileVisibility.Private && !isOwner)
        {
            isRestricted = true;
        }
        else if (profile.Visibility == ProfileVisibility.NetworkOnly && !isOwner && connectionStatus != "Accepted")
        {
            isRestricted = true;
        }

        if (isRestricted)
        {
            // Mask detailed sections for restricted viewers
            return new AcademicProfileDto(
                user.Id,
                user.UserName ?? string.Empty,
                profile.DisplayName,
                profile.Headline,
                null, // Masked bio
                null, // Masked country
                null, // Masked city
                profile.PreferredLanguage,
                profile.AvatarUrl,
                null, // Masked banner
                profile.Visibility.ToString(),
                profile.UpdatedAtUtc,
                completenessScore,
                isVerified,
                null, // Masked ORCID
                Array.Empty<UserEducationDto>(),
                Array.Empty<UserExperienceDto>(),
                Array.Empty<UserSkillDto>(),
                Array.Empty<string>(),
                Array.Empty<AffiliationDto>(),
                publicationsCount,
                connectionsCount,
                followersCount,
                followingCount,
                connectionStatus,
                isFollowing,
                false
            );
        }

        var educations = user.Educations
            .OrderByDescending(e => e.EndYear ?? e.StartYear ?? 0)
            .Select(e => new UserEducationDto(e.Id, e.InstitutionName, e.Degree, e.FieldOfStudy, e.StartYear, e.EndYear, e.Grade))
            .ToList();

        var experiences = user.Experiences
            .OrderByDescending(e => e.IsCurrent ? 1 : 0)
            .ThenByDescending(e => e.StartDate ?? DateTime.MinValue)
            .Select(e => new UserExperienceDto(e.Id, e.OrganizationName, e.RoleTitle, e.EmploymentType, e.StartDate, e.EndDate, e.IsCurrent, e.Description))
            .ToList();

        var skills = user.Skills
            .OrderBy(s => s.SkillName)
            .Select(s => new UserSkillDto(s.Id, s.SkillName))
            .ToList();

        var interests = user.Interests
            .OrderBy(i => i.Topic)
            .Select(i => i.Topic)
            .ToList();

        var affiliations = user.Memberships
            .Where(m => m.Organization != null)
            .Select(m => new AffiliationDto(
                m.OrganizationId,
                m.Organization!.Name,
                m.Department,
                m.Title,
                m.Role.ToString(),
                m.Organization.VerificationStatus == OrganizationVerificationStatus.Verified
            ))
            .ToList();

        return new AcademicProfileDto(
            user.Id,
            user.UserName ?? string.Empty,
            profile.DisplayName,
            profile.Headline,
            profile.Bio,
            profile.CountryCode,
            profile.City,
            profile.PreferredLanguage,
            profile.AvatarUrl,
            profile.BannerUrl,
            profile.Visibility.ToString(),
            profile.UpdatedAtUtc,
            completenessScore,
            isVerified,
            null, // ORCID placeholder (documented gap: external scholarly identifier OAuth integration)
            educations,
            experiences,
            skills,
            interests,
            affiliations,
            publicationsCount,
            connectionsCount,
            followersCount,
            followingCount,
            connectionStatus,
            isFollowing,
            isOwner
        );
    }

    public async Task<UserEducationDto> AddEducationAsync(Guid userId, CreateEducationRequest request, CancellationToken cancellationToken = default)
    {
        var edu = new UserEducation
        {
            UserId = userId,
            InstitutionName = request.InstitutionName.Trim(),
            Degree = request.Degree?.Trim(),
            FieldOfStudy = request.FieldOfStudy?.Trim(),
            StartYear = request.StartYear,
            EndYear = request.EndYear,
            Grade = request.Grade?.Trim()
        };

        _dbContext.UserEducations.Add(edu);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UserEducationDto(edu.Id, edu.InstitutionName, edu.Degree, edu.FieldOfStudy, edu.StartYear, edu.EndYear, edu.Grade);
    }

    public async Task<bool> DeleteEducationAsync(Guid userId, Guid educationId, CancellationToken cancellationToken = default)
    {
        var edu = await _dbContext.UserEducations
            .FirstOrDefaultAsync(e => e.Id == educationId && e.UserId == userId, cancellationToken);

        if (edu == null)
            return false;

        _dbContext.UserEducations.Remove(edu);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<UserExperienceDto> AddExperienceAsync(Guid userId, CreateExperienceRequest request, CancellationToken cancellationToken = default)
    {
        var exp = new UserExperience
        {
            UserId = userId,
            OrganizationName = request.OrganizationName.Trim(),
            RoleTitle = request.RoleTitle.Trim(),
            EmploymentType = request.EmploymentType?.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsCurrent = request.IsCurrent,
            Description = request.Description?.Trim()
        };

        _dbContext.UserExperiences.Add(exp);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UserExperienceDto(exp.Id, exp.OrganizationName, exp.RoleTitle, exp.EmploymentType, exp.StartDate, exp.EndDate, exp.IsCurrent, exp.Description);
    }

    public async Task<bool> DeleteExperienceAsync(Guid userId, Guid experienceId, CancellationToken cancellationToken = default)
    {
        var exp = await _dbContext.UserExperiences
            .FirstOrDefaultAsync(e => e.Id == experienceId && e.UserId == userId, cancellationToken);

        if (exp == null)
            return false;

        _dbContext.UserExperiences.Remove(exp);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<UserSkillDto> AddSkillAsync(Guid userId, string skillName, CancellationToken cancellationToken = default)
    {
        var normalized = skillName.Trim();
        var existing = await _dbContext.UserSkills
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SkillName.ToLower() == normalized.ToLower(), cancellationToken);

        if (existing != null)
            return new UserSkillDto(existing.Id, existing.SkillName);

        var skill = new UserSkill
        {
            UserId = userId,
            SkillName = normalized
        };

        _dbContext.UserSkills.Add(skill);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UserSkillDto(skill.Id, skill.SkillName);
    }

    public async Task<bool> DeleteSkillAsync(Guid userId, Guid skillId, CancellationToken cancellationToken = default)
    {
        var skill = await _dbContext.UserSkills
            .FirstOrDefaultAsync(s => s.Id == skillId && s.UserId == userId, cancellationToken);

        if (skill == null)
            return false;

        _dbContext.UserSkills.Remove(skill);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ResearcherDiscoveryDto>> DiscoverResearchersAsync(
        Guid? callerUserId,
        string? query = null,
        string? institution = null,
        string? expertise = null,
        int limit = 20,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 50);
        offset = Math.Max(0, offset);

        var blockedUserIds = new HashSet<Guid>();
        if (callerUserId.HasValue)
        {
            var callerId = callerUserId.Value;
            var blocks = await _dbContext.UserBlocks
                .AsNoTracking()
                .Where(b => b.BlockerUserId == callerId || b.BlockedUserId == callerId)
                .Select(b => b.BlockerUserId == callerId ? b.BlockedUserId : b.BlockerUserId)
                .ToListAsync(cancellationToken);

            foreach (var id in blocks)
            {
                blockedUserIds.Add(id);
            }
        }

        var usersQuery = _dbContext.Users
            .AsNoTracking()
            .Include(u => u.Profile)
            .Include(u => u.Skills)
            .Include(u => u.Memberships)
                .ThenInclude(m => m.Organization)
            .Where(u => u.Profile != null && u.Profile.Visibility != ProfileVisibility.Private);

        if (callerUserId.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.Id != callerUserId.Value && !blockedUserIds.Contains(u.Id));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim().ToLower();
            usersQuery = usersQuery.Where(u =>
                u.Profile!.DisplayName.ToLower().Contains(q) ||
                (u.UserName != null && u.UserName.ToLower().Contains(q)) ||
                (u.Profile.Headline != null && u.Profile.Headline.ToLower().Contains(q)));
        }

        if (!string.IsNullOrWhiteSpace(institution))
        {
            var inst = institution.Trim().ToLower();
            usersQuery = usersQuery.Where(u =>
                u.Memberships.Any(m => m.Organization != null && m.Organization.Name.ToLower().Contains(inst)));
        }

        if (!string.IsNullOrWhiteSpace(expertise))
        {
            var exp = expertise.Trim().ToLower();
            usersQuery = usersQuery.Where(u =>
                u.Skills.Any(s => s.SkillName.ToLower().Contains(exp)));
        }

        var candidates = await usersQuery
            .OrderBy(u => u.Profile!.DisplayName)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (!candidates.Any())
            return Array.Empty<ResearcherDiscoveryDto>();

        var candidateIds = candidates.Select(c => c.Id).ToList();

        // 1. Connection states for caller
        var connectionMap = new Dictionary<Guid, (string Status, bool IsRequester)>();
        if (callerUserId.HasValue)
        {
            var callerId = callerUserId.Value;
            var connections = await _dbContext.Connections
                .AsNoTracking()
                .Where(c => (c.CanonicalUserA == callerId && candidateIds.Contains(c.CanonicalUserB)) ||
                            (c.CanonicalUserB == callerId && candidateIds.Contains(c.CanonicalUserA)))
                .ToListAsync(cancellationToken);

            foreach (var conn in connections)
            {
                var otherId = conn.GetOtherUserId(callerId);
                connectionMap[otherId] = (conn.Status.ToString(), conn.RequesterUserId == callerId);
            }
        }

        // 2. Following states for caller
        var followingSet = new HashSet<Guid>();
        if (callerUserId.HasValue)
        {
            var callerId = callerUserId.Value;
            var follows = await _dbContext.Follows
                .AsNoTracking()
                .Where(f => f.FollowerUserId == callerId &&
                            f.TargetType == FollowTargetType.User &&
                            candidateIds.Contains(f.TargetId))
                .Select(f => f.TargetId)
                .ToListAsync(cancellationToken);

            foreach (var fId in follows)
            {
                followingSet.Add(fId);
            }
        }

        // 3. Publications count
        var publicationsMap = await _dbContext.Posts
            .AsNoTracking()
            .Where(p => candidateIds.Contains(p.AuthorUserId) &&
                        p.Status == PostStatus.Published &&
                        p.Visibility == PostVisibility.Public)
            .GroupBy(p => p.AuthorUserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.UserId, g => g.Count, cancellationToken);

        // 4. Connections count
        var connectionsMap = await _dbContext.Connections
            .AsNoTracking()
            .Where(c => c.Status == ConnectionStatus.Accepted &&
                        (candidateIds.Contains(c.CanonicalUserA) || candidateIds.Contains(c.CanonicalUserB)))
            .ToListAsync(cancellationToken);

        var connCountMap = new Dictionary<Guid, int>();
        foreach (var c in candidateIds)
        {
            connCountMap[c] = connectionsMap.Count(conn => conn.CanonicalUserA == c || conn.CanonicalUserB == c);
        }

        var result = new List<ResearcherDiscoveryDto>();
        foreach (var user in candidates)
        {
            var p = user.Profile!;
            var primaryOrg = user.Memberships
                .Where(m => m.Organization != null)
                .Select(m => m.Organization!.Name)
                .FirstOrDefault();

            var isVerified = user.Memberships
                .Any(m => m.Organization != null && m.Organization.VerificationStatus == OrganizationVerificationStatus.Verified);

            var topSkills = user.Skills
                .OrderBy(s => s.SkillName)
                .Take(4)
                .Select(s => s.SkillName)
                .ToList();

            var connStatus = "None";
            if (connectionMap.TryGetValue(user.Id, out var state))
            {
                if (state.Status == "Accepted")
                    connStatus = "Accepted";
                else if (state.Status == "Pending")
                    connStatus = state.IsRequester ? "PendingOutgoing" : "PendingIncoming";
                else
                    connStatus = state.Status;
            }

            var isFollowing = followingSet.Contains(user.Id);
            publicationsMap.TryGetValue(user.Id, out var pubCount);
            connCountMap.TryGetValue(user.Id, out var conCount);

            result.Add(new ResearcherDiscoveryDto(
                user.Id,
                user.UserName ?? string.Empty,
                p.DisplayName,
                p.Headline,
                p.AvatarUrl,
                primaryOrg,
                isVerified,
                topSkills,
                pubCount,
                conCount,
                connStatus,
                isFollowing
            ));
        }

        return result;
    }
}
