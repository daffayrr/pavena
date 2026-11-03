using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pavena.Application.Challenges.DTOs;
using Pavena.Application.Challenges.Interfaces;
using Pavena.Application.Common.Interfaces;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Challenges;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Enums;

namespace Pavena.Infrastructure.Challenges;

public class ChallengeAttachmentService : IChallengeAttachmentService
{
    private const long MaxAttachmentSizeBytes = 50 * 1024 * 1024; // 50 MB

    private readonly IPavenaDbContext _dbContext;
    private readonly IObjectStorageService _storageService;
    private readonly IChallengeNdaService _ndaService;
    private readonly ILogger<ChallengeAttachmentService> _logger;

    public ChallengeAttachmentService(
        IPavenaDbContext dbContext,
        IObjectStorageService storageService,
        IChallengeNdaService ndaService,
        ILogger<ChallengeAttachmentService> logger)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _ndaService = ndaService;
        _logger = logger;
    }

    public async Task<ChallengeAttachmentDto> UploadAttachmentAsync(
        Guid currentUserId,
        Guid challengeId,
        string fileName,
        Stream fileStream,
        string mimeType,
        AttachmentConfidentialityLevel confidentiality,
        string? description = null,
        CancellationToken ct = default)
    {
        var challenge = await _dbContext.IndustryChallenges
            .Include(c => c.Organization)
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        if (challenge == null)
            throw new KeyNotFoundException("Challenge not found.");

        await AssertCanManageChallengeAttachmentsAsync(currentUserId, challenge, ct);

        if (fileStream.Length > MaxAttachmentSizeBytes)
        {
            throw new DomainException($"File size {fileStream.Length / (1024 * 1024)} MB exceeds maximum permitted limit of {MaxAttachmentSizeBytes / (1024 * 1024)} MB.");
        }

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var safeFileName = Path.GetFileName(fileName);
        var attachmentId = Guid.NewGuid();
        var storageKey = $"challenges/{challengeId}/attachments/{attachmentId}/{safeFileName}";

        await _storageService.SaveObjectAsync(storageKey, fileStream, mimeType, ct);

        var attachment = ChallengeAttachment.Create(
            challengeId,
            safeFileName,
            ext,
            mimeType,
            fileStream.Length,
            storageKey,
            confidentiality,
            currentUserId,
            description
        );

        _dbContext.ChallengeAttachments.Add(attachment);

        challenge.LogActivity(
            currentUserId,
            "AttachmentAdded",
            $"Added {(confidentiality == AttachmentConfidentialityLevel.ConfidentialNdaRequired ? "confidential" : "public")} attachment '{safeFileName}'."
        );

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Attachment {AttachmentId} '{FileName}' uploaded for challenge {ChallengeId}.",
            attachment.Id, attachment.FileName, challengeId);

        return new ChallengeAttachmentDto(
            attachment.Id,
            attachment.ChallengeId,
            attachment.FileName,
            attachment.FileExtension,
            attachment.MimeType,
            attachment.SizeBytes,
            attachment.ConfidentialityLevel,
            attachment.Description,
            true,
            attachment.CreatedAtUtc
        );
    }

    public async Task DeleteAttachmentAsync(Guid currentUserId, Guid challengeId, Guid attachmentId, CancellationToken ct = default)
    {
        var challenge = await _dbContext.IndustryChallenges
            .Include(c => c.Organization)
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        if (challenge == null)
            throw new KeyNotFoundException("Challenge not found.");

        await AssertCanManageChallengeAttachmentsAsync(currentUserId, challenge, ct);

        var attachment = await _dbContext.ChallengeAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.ChallengeId == challengeId, ct);

        if (attachment == null)
            throw new KeyNotFoundException("Attachment not found.");

        await _storageService.DeleteObjectAsync(attachment.StorageKey, ct);
        _dbContext.ChallengeAttachments.Remove(attachment);

        challenge.LogActivity(
            currentUserId,
            "AttachmentDeleted",
            $"Deleted attachment '{attachment.FileName}'."
        );

        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Attachment {AttachmentId} deleted from challenge {ChallengeId}.", attachmentId, challengeId);
    }

    public async Task<Stream> DownloadAttachmentStreamAsync(Guid? currentUserId, Guid challengeId, Guid attachmentId, CancellationToken ct = default)
    {
        var challenge = await _dbContext.IndustryChallenges
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        if (challenge == null)
            throw new KeyNotFoundException("Challenge not found.");

        var attachment = await _dbContext.ChallengeAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.ChallengeId == challengeId, ct);

        if (attachment == null)
            throw new KeyNotFoundException("Attachment not found.");

        // Check view permission for challenge
        if (challenge.Visibility == ChallengeVisibility.AuthenticatedUsersOnly && !currentUserId.HasValue)
        {
            throw new KeyNotFoundException("Challenge not found.");
        }

        // Tier 2 confidentiality check
        if (attachment.ConfidentialityLevel == AttachmentConfidentialityLevel.ConfidentialNdaRequired)
        {
            if (!currentUserId.HasValue)
            {
                throw new KeyNotFoundException("Attachment not found.");
            }

            var hasClearance = await _ndaService.HasValidNdaClearanceAsync(currentUserId.Value, challengeId, ct);
            if (!hasClearance)
            {
                throw new UnauthorizedAccessException("Confidential attachment access requires a signed and approved NDA agreement.");
            }
        }

        return await _storageService.GetObjectStreamAsync(attachment.StorageKey, ct);
    }

    private async Task AssertCanManageChallengeAttachmentsAsync(Guid currentUserId, IndustryChallenge challenge, CancellationToken ct)
    {
        var isPlatformAdmin = await IsPlatformAdminAsync(currentUserId, ct);
        if (isPlatformAdmin) return;

        var membership = await _dbContext.OrganizationMemberships
            .FirstOrDefaultAsync(m => m.OrganizationId == challenge.OrganizationId && m.UserId == currentUserId && m.IsActive, ct);

        if (membership == null)
            throw new UnauthorizedAccessException("You are not an active member of the owning organization.");

        var isOwnerOrAdmin = membership.Role == OrgMembershipRole.OrganizationAdmin;
        var isCreator = challenge.CreatedByUserId == currentUserId;

        if (!isOwnerOrAdmin && !isCreator)
            throw new UnauthorizedAccessException("You do not have permission to modify attachments for this challenge.");
    }

    private async Task<bool> IsPlatformAdminAsync(Guid userId, CancellationToken ct)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return false;

        var roles = await (from ur in _dbContext.UserPlatformRoles
                           join r in _dbContext.PlatformRoles on ur.RoleId equals r.Id
                           where ur.UserId == userId
                           select r.Name).ToListAsync(ct);

        return roles.Contains(PlatformRole.Superadmin) || roles.Contains(PlatformRole.PlatformAdmin);
    }
}
