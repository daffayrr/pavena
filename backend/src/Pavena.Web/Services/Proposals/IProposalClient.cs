using Pavena.Application.Proposals.DTOs;
using Pavena.Domain.Enums;

namespace Pavena.Web.Services.Proposals;

public interface IProposalClient
{
    Task<ProposalDetailResponse?> CreateDraftAsync(CreateProposalDraftRequest request, CancellationToken ct = default);
    Task<ProposalDetailResponse?> UpdateDraftAsync(Guid id, UpdateProposalDraftRequest request, CancellationToken ct = default);
    Task<ProposalDetailResponse?> SubmitAsync(Guid id, CancellationToken ct = default);
    Task<ProposalDetailResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ProposalSummaryResponse>> GetSummariesAsync(
        Guid? challengeId = null,
        Guid? leadResearcherId = null,
        ProposalStatus? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);
    Task<ProposalDetailResponse?> AddInvestigatorAsync(Guid id, AddInvestigatorRequest request, CancellationToken ct = default);
    Task<ProposalDetailResponse?> RemoveInvestigatorAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<ProposalAttachmentResponse?> UploadAttachmentAsync(
        Guid id,
        string fileName,
        byte[] fileBytes,
        string contentType,
        ProposalAttachmentType type = ProposalAttachmentType.TechnicalProposal,
        CancellationToken ct = default);
    Task<ProposalDetailResponse?> RemoveAttachmentAsync(Guid id, Guid attachmentId, CancellationToken ct = default);
    Task<ProposalDetailResponse?> ResubmitAsync(Guid id, ResubmitProposalRequest request, CancellationToken ct = default);
    Task<ProposalDetailResponse?> WithdrawAsync(Guid id, string? reason, CancellationToken ct = default);
    Task<EligibilityCheckResult?> CheckEligibilityAsync(Guid id, CancellationToken ct = default);
    Task<ProposalDetailResponse?> ReviewEligibilityAsync(Guid id, ReviewEligibilityRequest request, CancellationToken ct = default);
    Task<ReviewAssignmentResponse?> AssignReviewerAsync(Guid id, AssignReviewerRequest request, CancellationToken ct = default);
    Task<ReviewAssignmentResponse?> RespondToAssignmentAsync(Guid assignmentId, bool accept, string? declineReason, CancellationToken ct = default);
    Task<ReviewAssignmentResponse?> DeclareCoiAsync(Guid assignmentId, DeclareCoiRequest request, CancellationToken ct = default);
    Task<ProposalAssessmentResponse?> SubmitAssessmentAsync(Guid assignmentId, SubmitAssessmentRequest request, CancellationToken ct = default);
    Task<List<ReviewAssignmentResponse>> GetMyReviewsAsync(CancellationToken ct = default);
    Task<ProposalDetailResponse?> ApplySponsorDecisionAsync(Guid id, SponsorDecisionRequest request, CancellationToken ct = default);
    Task<ProposalConversionResponse?> ConvertToProjectAsync(Guid id, ConvertProposalRequest request, CancellationToken ct = default);
}
