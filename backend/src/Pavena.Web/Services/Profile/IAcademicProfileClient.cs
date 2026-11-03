using Pavena.Application.Identity.DTOs;
using Pavena.Application.Social.DTOs;

namespace Pavena.Web.Services.Profile;

public interface IAcademicProfileClient
{
    // Profile
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

    // Posts & Content Management
    Task<PostDto?> CreatePostAsync(CreatePostRequest request, CancellationToken cancellationToken = default);
    Task<PostDto?> GetPostByIdAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<PostDto?> UpdatePostAsync(Guid postId, UpdatePostRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeletePostAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<PostDto?> PublishPostAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<PostDto?> ArchivePostAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PostDto>> GetMyPostsAsync(CancellationToken cancellationToken = default);

    // Feed (Chronological & Keyset Cursor Pagination)
    Task<FeedResponseDto?> GetMyNetworkFeedAsync(int limit = 20, string? cursor = null, CancellationToken cancellationToken = default);
    Task<FeedResponseDto?> GetDiscoverFeedAsync(int limit = 20, string? cursor = null, CancellationToken cancellationToken = default);

    // Researcher Discovery
    Task<IReadOnlyList<ResearcherDiscoveryDto>> GetDiscoverResearchersAsync(
        string? query = null,
        string? institution = null,
        string? expertise = null,
        int limit = 20,
        int offset = 0,
        CancellationToken cancellationToken = default);

    // Connections
    Task<IReadOnlyList<ConnectionUserDto>> GetAcceptedConnectionsAsync(CancellationToken cancellationToken = default);
    Task<bool> RemoveConnectionAsync(Guid targetUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConnectionDto>> GetIncomingConnectionRequestsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConnectionDto>> GetOutgoingConnectionRequestsAsync(CancellationToken cancellationToken = default);
    Task<bool> SendConnectionRequestAsync(Guid targetUserId, CancellationToken cancellationToken = default);
    Task<bool> CancelConnectionRequestAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<bool> AcceptConnectionRequestAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<bool> RejectConnectionRequestAsync(Guid requestId, CancellationToken cancellationToken = default);

    // Following & Followers
    Task<IReadOnlyList<FollowUserDto>> GetFollowingAsync(Guid? userId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FollowUserDto>> GetFollowersAsync(Guid? userId = null, CancellationToken cancellationToken = default);
    Task<bool> FollowUserAsync(Guid targetUserId, CancellationToken cancellationToken = default);
    Task<bool> UnfollowUserAsync(Guid targetUserId, CancellationToken cancellationToken = default);

    // Blocking
    Task<IReadOnlyList<BlockedUserDto>> GetBlockedUsersAsync(CancellationToken cancellationToken = default);
    Task<bool> BlockUserAsync(Guid targetUserId, string? reason = null, CancellationToken cancellationToken = default);
    Task<bool> UnblockUserAsync(Guid targetUserId, CancellationToken cancellationToken = default);
}
