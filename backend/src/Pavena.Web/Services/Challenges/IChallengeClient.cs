using Pavena.Application.Challenges.DTOs;
using Pavena.Domain.Enums;

namespace Pavena.Web.Services.Challenges;

public interface IChallengeClient
{
    Task<List<ChallengeSummaryDto>> SearchChallengesAsync(
        string? query = null,
        string? domain = null,
        ChallengeStatus? status = null,
        InstitutionalEligibility? eligibility = null,
        decimal? maxBudget = null,
        Guid? organizationId = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<ChallengeDetailDto?> GetChallengeByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ChallengeDetailDto?> CreateChallengeAsync(CreateChallengeRequest request, CancellationToken cancellationToken = default);

    Task<ChallengeDetailDto?> UpdateChallengeAsync(Guid id, UpdateChallengeRequest request, CancellationToken cancellationToken = default);

    Task<bool> SubmitForApprovalAsync(Guid id, string? notes = null, CancellationToken cancellationToken = default);

    Task<bool> ProcessApprovalDecisionAsync(Guid id, ChallengeDecisionRequest request, CancellationToken cancellationToken = default);

    Task<bool> PublishChallengeAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> OpenProposalsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> CloseChallengeAsync(Guid id, string? notes = null, CancellationToken cancellationToken = default);

    Task<bool> CancelChallengeAsync(Guid id, CancelChallengeRequest request, CancellationToken cancellationToken = default);

    Task<EligibilityCheckResultDto?> CheckEligibilityAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ChallengeNdaStatusDto?> GetNdaStatusAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ChallengeNdaStatusDto?> SignNdaAsync(Guid id, SignNdaRequest request, CancellationToken cancellationToken = default);

    Task<bool> ApproveNdaAsync(Guid id, Guid ndaId, CancellationToken cancellationToken = default);

    Task<ChallengeAttachmentDto?> UploadAttachmentAsync(
        Guid id,
        string fileName,
        byte[] fileBytes,
        string mimeType,
        AttachmentConfidentialityLevel confidentiality,
        string? description = null,
        CancellationToken cancellationToken = default);

    Task<byte[]?> DownloadAttachmentBytesAsync(Guid id, Guid attachmentId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAttachmentAsync(Guid id, Guid attachmentId, CancellationToken cancellationToken = default);

    Task<List<ChallengeActivityLogDto>> GetActivityLogsAsync(Guid id, CancellationToken cancellationToken = default);
}
