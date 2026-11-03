using Pavena.Application.Social.DTOs;

namespace Pavena.Application.Social.Interfaces;

public interface IFeedService
{
    Task<FeedResponseDto> GetMyNetworkFeedAsync(Guid callerUserId, int limit = 20, string? cursor = null, CancellationToken cancellationToken = default);
    Task<FeedResponseDto> GetDiscoverFeedAsync(Guid callerUserId, int limit = 20, string? cursor = null, CancellationToken cancellationToken = default);
}
