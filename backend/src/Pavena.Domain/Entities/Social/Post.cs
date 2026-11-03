using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Enums;

namespace Pavena.Domain.Entities.Social;

public class Post : BaseEntity
{
    public const int MaxContentLength = 5000;
    public const int MaxAttachmentsCount = 10;
    public const long MaxAttachmentSizeBytes = 25 * 1024 * 1024; // 25 MB

    public Guid AuthorUserId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public PostVisibility Visibility { get; private set; } = PostVisibility.Public;
    public PostStatus Status { get; private set; } = PostStatus.Draft;
    public PostCategory Category { get; private set; } = PostCategory.GeneralAcademic;
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? PublishedAtUtc { get; private set; }
    public DateTime? EditedAtUtc { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }
    public string ConcurrencyStamp { get; private set; } = Guid.NewGuid().ToString();

    public virtual User? AuthorUser { get; set; }
    public virtual ICollection<PostAttachment> Attachments { get; set; } = new List<PostAttachment>();

    // For EF Core
    public Post()
    {
    }

    public static Post CreateDraft(Guid authorId, string content, PostVisibility visibility, PostCategory category = PostCategory.GeneralAcademic)
    {
        ValidateAuthor(authorId);
        ValidateContentLength(content);

        return new Post
        {
            AuthorUserId = authorId,
            Content = content,
            Visibility = visibility,
            Category = category,
            Status = PostStatus.Draft,
            PublishedAtUtc = null,
            EditedAtUtc = null,
            IsDeleted = false,
            DeletedAtUtc = null
        };
    }

    public static Post CreatePublished(Guid authorId, string content, PostVisibility visibility, PostCategory category = PostCategory.GeneralAcademic)
    {
        ValidateAuthor(authorId);
        ValidatePublishedContent(content);

        var now = DateTime.UtcNow;
        return new Post
        {
            AuthorUserId = authorId,
            Content = content,
            Visibility = visibility,
            Category = category,
            Status = PostStatus.Published,
            PublishedAtUtc = now,
            EditedAtUtc = null,
            IsDeleted = false,
            DeletedAtUtc = null
        };
    }

    public void Publish(Guid callerUserId)
    {
        EnsureNotDeleted();
        EnsureAuthor(callerUserId);
        ValidatePublishedContent(Content);

        Status = PostStatus.Published;
        PublishedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    public void Archive(Guid callerUserId)
    {
        EnsureNotDeleted();
        EnsureAuthor(callerUserId);

        Status = PostStatus.Archived;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    public void UpdateContent(Guid callerUserId, string newContent, PostVisibility newVisibility, PostCategory? newCategory = null)
    {
        EnsureNotDeleted();
        EnsureAuthor(callerUserId);

        if (Status == PostStatus.Published)
        {
            ValidatePublishedContent(newContent);
        }
        else
        {
            ValidateContentLength(newContent);
        }

        Content = newContent;
        Visibility = newVisibility;
        if (newCategory.HasValue)
        {
            Category = newCategory.Value;
        }
        EditedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    public void SoftDelete(Guid callerUserId, bool isPrivileged = false)
    {
        EnsureNotDeleted();
        if (!isPrivileged)
        {
            EnsureAuthor(callerUserId);
        }

        IsDeleted = true;
        DeletedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    public PostAttachment AddAttachment(string fileName, string contentType, long sizeBytes, string storageKey)
    {
        EnsureNotDeleted();
        if (Attachments.Count >= MaxAttachmentsCount)
            throw new DomainException($"A post cannot have more than {MaxAttachmentsCount} attachments.");

        if (string.IsNullOrWhiteSpace(fileName))
            throw new DomainException("Attachment file name is required.");

        if (string.IsNullOrWhiteSpace(storageKey))
            throw new DomainException("Attachment storage key is required.");

        if (sizeBytes <= 0 || sizeBytes > MaxAttachmentSizeBytes)
            throw new DomainException($"Attachment file size must be between 1 byte and {MaxAttachmentSizeBytes / (1024 * 1024)} MB.");

        var attachment = new PostAttachment
        {
            PostId = Id,
            FileName = fileName.Trim(),
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim(),
            FileSizeBytes = sizeBytes,
            StorageKey = storageKey.Trim()
        };

        Attachments.Add(attachment);
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();
        return attachment;
    }

    public PostAttachment RemoveAttachment(Guid attachmentId, Guid callerUserId)
    {
        EnsureNotDeleted();
        EnsureAuthor(callerUserId);

        var attachment = Attachments.FirstOrDefault(a => a.Id == attachmentId)
            ?? throw new DomainException($"Attachment {attachmentId} was not found on this post.");

        Attachments.Remove(attachment);
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();
        return attachment;
    }

    private static void ValidateAuthor(Guid authorId)
    {
        if (authorId == Guid.Empty)
            throw new DomainException("Author user ID cannot be empty.");
    }

    private static void ValidateContentLength(string content)
    {
        if (content != null && content.Length > MaxContentLength)
            throw new DomainException($"Post content cannot exceed {MaxContentLength} characters.");
    }

    private static void ValidatePublishedContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Published post content cannot be empty or whitespace.");

        ValidateContentLength(content);
    }

    private void EnsureAuthor(Guid callerUserId)
    {
        if (callerUserId != AuthorUserId)
            throw new DomainException("Only the author has permission to perform this post action.");
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("Cannot perform actions on a deleted post.");
    }
}

public class PostAttachment : BaseEntity
{
    public Guid PostId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long FileSizeBytes { get; set; }
    public string StorageKey { get; set; } = string.Empty;

    public virtual Post? Post { get; set; }

    public PostAttachment()
    {
    }
}
