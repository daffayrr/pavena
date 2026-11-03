using Pavena.Application.Social.DTOs;

namespace Pavena.Application.Social.Interfaces;

public interface IPostService
{
    Task<PostDto> CreatePostAsync(Guid authorId, CreatePostRequest request, CancellationToken cancellationToken = default);
    Task<PostDto> GetPostByIdAsync(Guid callerUserId, Guid postId, CancellationToken cancellationToken = default);
    Task<PostDto> UpdatePostAsync(Guid callerUserId, Guid postId, UpdatePostRequest request, CancellationToken cancellationToken = default);
    Task DeletePostAsync(Guid callerUserId, Guid postId, CancellationToken cancellationToken = default);
    Task<PostDto> PublishPostAsync(Guid callerUserId, Guid postId, CancellationToken cancellationToken = default);
    Task<PostDto> ArchivePostAsync(Guid callerUserId, Guid postId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PostDto>> GetUserPostsAsync(Guid callerUserId, Guid targetUserId, CancellationToken cancellationToken = default);
    Task<(string StorageKey, string FileName, string ContentType, long FileSizeBytes)> GetAttachmentDownloadInfoAsync(Guid callerUserId, Guid postId, Guid attachmentId, CancellationToken cancellationToken = default);
    Task DeleteAttachmentAsync(Guid callerUserId, Guid postId, Guid attachmentId, CancellationToken cancellationToken = default);
}
