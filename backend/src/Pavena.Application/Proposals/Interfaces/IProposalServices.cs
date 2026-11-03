using Pavena.Application.Proposals.DTOs;
using Pavena.Domain.Enums;

namespace Pavena.Application.Proposals.Interfaces;

public interface IProposalService
{
    Task<ProposalDetailResponse> CreateDraftAsync(CreateProposalDraftRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<ProposalDetailResponse> UpdateDraftAsync(Guid proposalId, UpdateProposalDraftRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<ProposalDetailResponse> SubmitAsync(Guid proposalId, Guid actorUserId, CancellationToken ct = default);
    Task<ProposalDetailResponse?> GetByIdAsync(Guid proposalId, Guid actorUserId, CancellationToken ct = default);
    Task<List<ProposalSummaryResponse>> GetSummariesAsync(Guid? challengeId, Guid? leadResearcherId, ProposalStatus? status, int page, int pageSize, Guid actorUserId, CancellationToken ct = default);
    Task<ProposalDetailResponse> AddInvestigatorAsync(Guid proposalId, AddInvestigatorRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<ProposalDetailResponse> RemoveInvestigatorAsync(Guid proposalId, Guid investigatorUserId, Guid actorUserId, CancellationToken ct = default);
    Task<ProposalAttachmentResponse> UploadAttachmentAsync(Guid proposalId, ProposalAttachmentType type, string fileName, string contentType, Stream stream, Guid actorUserId, CancellationToken ct = default);
    Task<ProposalDetailResponse> RemoveAttachmentAsync(Guid proposalId, Guid attachmentId, Guid actorUserId, CancellationToken ct = default);
    Task<ProposalDetailResponse> ResubmitAsync(Guid proposalId, ResubmitProposalRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<ProposalDetailResponse> WithdrawAsync(Guid proposalId, string? reason, Guid actorUserId, CancellationToken ct = default);
}

public interface IProposalEligibilityService
{
    Task<EligibilityCheckResult> EvaluateEligibilityAsync(Guid proposalId, Guid actorUserId, CancellationToken ct = default);
    Task<ProposalDetailResponse> RecordEligibilityReviewAsync(Guid proposalId, ReviewEligibilityRequest request, Guid actorUserId, CancellationToken ct = default);
}

public interface IProposalReviewService
{
    Task<ReviewAssignmentResponse> AssignReviewerAsync(Guid proposalId, AssignReviewerRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<ReviewAssignmentResponse> RespondToAssignmentAsync(Guid assignmentId, bool accept, string? declineReason, Guid reviewerUserId, CancellationToken ct = default);
    Task<ReviewAssignmentResponse> DeclareCoiAsync(Guid assignmentId, DeclareCoiRequest request, Guid reviewerUserId, CancellationToken ct = default);
    Task<ProposalAssessmentResponse> SubmitAssessmentAsync(Guid assignmentId, SubmitAssessmentRequest request, Guid reviewerUserId, CancellationToken ct = default);
    Task<List<ReviewAssignmentResponse>> GetReviewerAssignmentsAsync(Guid reviewerUserId, CancellationToken ct = default);
}

public interface IProposalConversionService
{
    Task<ProposalDetailResponse> ApplySponsorDecisionAsync(Guid proposalId, SponsorDecisionRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<ProposalConversionResponse> ConvertToProjectAsync(Guid proposalId, ConvertProposalRequest request, Guid actorUserId, CancellationToken ct = default);
}
