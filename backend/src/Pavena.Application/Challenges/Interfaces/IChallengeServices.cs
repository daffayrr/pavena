using Pavena.Application.Challenges.DTOs;
using Pavena.Domain.Enums;

namespace Pavena.Application.Challenges.Interfaces;

public interface IChallengeService
{
    Task<ChallengeDetailDto> CreateChallengeAsync(Guid currentUserId, CreateChallengeRequest request, CancellationToken ct = default);
    Task<ChallengeDetailDto> UpdateChallengeAsync(Guid currentUserId, Guid challengeId, UpdateChallengeRequest request, CancellationToken ct = default);
    Task<ChallengeDetailDto> GetChallengeByIdAsync(Guid? currentUserId, Guid challengeId, CancellationToken ct = default);
    Task<ChallengeDetailDto> GetChallengeBySlugAsync(Guid? currentUserId, string slug, CancellationToken ct = default);
    Task<List<ChallengeSummaryDto>> SearchChallengesAsync(
        Guid? currentUserId,
        string? query = null,
        string? domain = null,
        ChallengeStatus? status = null,
        InstitutionalEligibility? eligibility = null,
        decimal? maxBudget = null,
        Guid? organizationId = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);
    Task SubmitForApprovalAsync(Guid currentUserId, Guid challengeId, string? notes = null, CancellationToken ct = default);
    Task ProcessApprovalDecisionAsync(Guid currentUserId, Guid challengeId, ChallengeDecisionRequest request, CancellationToken ct = default);
    Task PublishChallengeAsync(Guid currentUserId, Guid challengeId, CancellationToken ct = default);
    Task OpenForProposalsAsync(Guid currentUserId, Guid challengeId, CancellationToken ct = default);
    Task CloseChallengeAsync(Guid currentUserId, Guid challengeId, string? notes = null, CancellationToken ct = default);
    Task CancelChallengeAsync(Guid currentUserId, Guid challengeId, string reason, CancellationToken ct = default);
    Task DeleteDraftChallengeAsync(Guid currentUserId, Guid challengeId, CancellationToken ct = default);
    Task<EligibilityCheckResultDto> CheckEligibilityAsync(Guid currentUserId, Guid challengeId, CancellationToken ct = default);
    Task<List<ChallengeActivityLogDto>> GetActivityLogsAsync(Guid currentUserId, Guid challengeId, CancellationToken ct = default);
}

public interface IChallengeAttachmentService
{
    Task<ChallengeAttachmentDto> UploadAttachmentAsync(
        Guid currentUserId,
        Guid challengeId,
        string fileName,
        Stream fileStream,
        string mimeType,
        AttachmentConfidentialityLevel confidentiality,
        string? description = null,
        CancellationToken ct = default);

    Task DeleteAttachmentAsync(Guid currentUserId, Guid challengeId, Guid attachmentId, CancellationToken ct = default);
    Task<Stream> DownloadAttachmentStreamAsync(Guid? currentUserId, Guid challengeId, Guid attachmentId, CancellationToken ct = default);
}

public interface IChallengeNdaService
{
    Task<ChallengeNdaStatusDto> GetNdaStatusAsync(Guid currentUserId, Guid challengeId, CancellationToken ct = default);
    Task<ChallengeNdaStatusDto> SignNdaAsync(Guid currentUserId, Guid challengeId, SignNdaRequest request, string? clientIp = null, CancellationToken ct = default);
    Task ApproveNdaAsync(Guid sponsorUserId, Guid challengeId, Guid ndaAgreementId, CancellationToken ct = default);
    Task<bool> HasValidNdaClearanceAsync(Guid userId, Guid challengeId, CancellationToken ct = default);
}
