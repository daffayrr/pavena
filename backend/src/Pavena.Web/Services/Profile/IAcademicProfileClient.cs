using Pavena.Application.Identity.DTOs;
using Pavena.Application.Social.DTOs;

namespace Pavena.Web.Services.Profile;

public interface IAcademicProfileClient
{
    Task<AcademicProfileDto?> GetMyAcademicProfileAsync(CancellationToken cancellationToken = default);
    Task<AcademicProfileDto?> GetAcademicProfileAsync(string identifier, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<UserEducationDto?> AddEducationAsync(CreateEducationRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteEducationAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserExperienceDto?> AddExperienceAsync(CreateExperienceRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteExperienceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserSkillDto?> AddSkillAsync(string skillName, CancellationToken cancellationToken = default);
    Task<bool> DeleteSkillAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<PostDto>> GetUserPostsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> SendConnectionRequestAsync(Guid targetUserId, CancellationToken cancellationToken = default);
    Task<bool> CancelConnectionRequestAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<bool> AcceptConnectionRequestAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<bool> FollowUserAsync(Guid targetUserId, CancellationToken cancellationToken = default);
    Task<bool> UnfollowUserAsync(Guid targetUserId, CancellationToken cancellationToken = default);
}
