using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pavena.Application.Proposals.DTOs;
using Pavena.Application.Proposals.Interfaces;
using Pavena.Domain.Enums;

namespace Pavena.API.Controllers;

[ApiController]
[Route("api/v1/proposals")]
[Authorize]
public class ProposalsController : ControllerBase
{
    private readonly IProposalService _proposalService;
    private readonly IProposalEligibilityService _eligibilityService;
    private readonly IProposalReviewService _reviewService;
    private readonly IProposalConversionService _conversionService;
    private readonly IValidator<CreateProposalDraftRequest> _createValidator;
    private readonly IValidator<UpdateProposalDraftRequest> _updateValidator;
    private readonly IValidator<AddInvestigatorRequest> _investigatorValidator;
    private readonly IValidator<ResubmitProposalRequest> _resubmitValidator;
    private readonly IValidator<SubmitAssessmentRequest> _assessmentValidator;
    private readonly IValidator<SponsorDecisionRequest> _decisionValidator;
    private readonly IValidator<DeclareCoiRequest> _coiValidator;

    public ProposalsController(
        IProposalService proposalService,
        IProposalEligibilityService eligibilityService,
        IProposalReviewService reviewService,
        IProposalConversionService conversionService,
        IValidator<CreateProposalDraftRequest> createValidator,
        IValidator<UpdateProposalDraftRequest> updateValidator,
        IValidator<AddInvestigatorRequest> investigatorValidator,
        IValidator<ResubmitProposalRequest> resubmitValidator,
        IValidator<SubmitAssessmentRequest> assessmentValidator,
        IValidator<SponsorDecisionRequest> decisionValidator,
        IValidator<DeclareCoiRequest> coiValidator)
    {
        _proposalService = proposalService;
        _eligibilityService = eligibilityService;
        _reviewService = reviewService;
        _conversionService = conversionService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _investigatorValidator = investigatorValidator;
        _resubmitValidator = resubmitValidator;
        _assessmentValidator = assessmentValidator;
        _decisionValidator = decisionValidator;
        _coiValidator = coiValidator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateDraft([FromBody] CreateProposalDraftRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.ToDictionary());

        var proposal = await _proposalService.CreateDraftAsync(request, callerUserId.Value, ct);
        return CreatedAtAction(nameof(GetById), new { id = proposal.Id }, proposal);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDraft([FromRoute] Guid id, [FromBody] UpdateProposalDraftRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.ToDictionary());

        var proposal = await _proposalService.UpdateDraftAsync(id, request, callerUserId.Value, ct);
        return Ok(proposal);
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit([FromRoute] Guid id, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var proposal = await _proposalService.SubmitAsync(id, callerUserId.Value, ct);
        return Ok(proposal);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var proposal = await _proposalService.GetByIdAsync(id, callerUserId.Value, ct);
        if (proposal == null)
        {
            // 404 Concealment to prevent IDOR / enumeration attacks
            return NotFound();
        }

        return Ok(proposal);
    }

    [HttpGet]
    public async Task<IActionResult> GetSummaries(
        [FromQuery] Guid? challengeId,
        [FromQuery] Guid? leadResearcherId,
        [FromQuery] ProposalStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var proposals = await _proposalService.GetSummariesAsync(
            challengeId, leadResearcherId, status, page, pageSize, callerUserId.Value, ct);

        return Ok(proposals);
    }

    [HttpPost("{id:guid}/investigators")]
    public async Task<IActionResult> AddInvestigator([FromRoute] Guid id, [FromBody] AddInvestigatorRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var validation = await _investigatorValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.ToDictionary());

        var proposal = await _proposalService.AddInvestigatorAsync(id, request, callerUserId.Value, ct);
        return Ok(proposal);
    }

    [HttpDelete("{id:guid}/investigators/{userId:guid}")]
    public async Task<IActionResult> RemoveInvestigator([FromRoute] Guid id, [FromRoute] Guid userId, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var proposal = await _proposalService.RemoveInvestigatorAsync(id, userId, callerUserId.Value, ct);
        return Ok(proposal);
    }

    [HttpPost("{id:guid}/attachments")]
    public async Task<IActionResult> UploadAttachment(
        [FromRoute] Guid id,
        [FromForm] IFormFile file,
        [FromForm] ProposalAttachmentType type = ProposalAttachmentType.TechnicalProposal,
        CancellationToken ct = default)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest("File is empty.");

        await using var stream = file.OpenReadStream();
        var result = await _proposalService.UploadAttachmentAsync(
            id, type, file.FileName, file.ContentType, stream, callerUserId.Value, ct);

        return Ok(result);
    }

    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> RemoveAttachment([FromRoute] Guid id, [FromRoute] Guid attachmentId, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var proposal = await _proposalService.RemoveAttachmentAsync(id, attachmentId, callerUserId.Value, ct);
        return Ok(proposal);
    }

    [HttpPost("{id:guid}/resubmit")]
    public async Task<IActionResult> Resubmit([FromRoute] Guid id, [FromBody] ResubmitProposalRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var validation = await _resubmitValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.ToDictionary());

        var proposal = await _proposalService.ResubmitAsync(id, request, callerUserId.Value, ct);
        return Ok(proposal);
    }

    [HttpPost("{id:guid}/withdraw")]
    public async Task<IActionResult> Withdraw([FromRoute] Guid id, [FromBody] string? reason, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var proposal = await _proposalService.WithdrawAsync(id, reason, callerUserId.Value, ct);
        return Ok(proposal);
    }

    [HttpGet("{id:guid}/eligibility")]
    public async Task<IActionResult> CheckEligibility([FromRoute] Guid id, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var result = await _eligibilityService.EvaluateEligibilityAsync(id, callerUserId.Value, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/eligibility-review")]
    public async Task<IActionResult> ReviewEligibility([FromRoute] Guid id, [FromBody] ReviewEligibilityRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var proposal = await _eligibilityService.RecordEligibilityReviewAsync(id, request, callerUserId.Value, ct);
        return Ok(proposal);
    }

    [HttpPost("{id:guid}/reviewers")]
    public async Task<IActionResult> AssignReviewer([FromRoute] Guid id, [FromBody] AssignReviewerRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var assignment = await _reviewService.AssignReviewerAsync(id, request, callerUserId.Value, ct);
        return Ok(assignment);
    }

    [HttpPost("assignments/{assignmentId:guid}/respond")]
    public async Task<IActionResult> RespondToAssignment(
        [FromRoute] Guid assignmentId,
        [FromQuery] bool accept,
        [FromQuery] string? declineReason,
        CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var result = await _reviewService.RespondToAssignmentAsync(assignmentId, accept, declineReason, callerUserId.Value, ct);
        return Ok(result);
    }

    [HttpPost("assignments/{assignmentId:guid}/coi")]
    public async Task<IActionResult> DeclareCoi([FromRoute] Guid assignmentId, [FromBody] DeclareCoiRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var validation = await _coiValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.ToDictionary());

        var result = await _reviewService.DeclareCoiAsync(assignmentId, request, callerUserId.Value, ct);
        return Ok(result);
    }

    [HttpPost("assignments/{assignmentId:guid}/assessment")]
    public async Task<IActionResult> SubmitAssessment([FromRoute] Guid assignmentId, [FromBody] SubmitAssessmentRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var validation = await _assessmentValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.ToDictionary());

        var assessment = await _reviewService.SubmitAssessmentAsync(assignmentId, request, callerUserId.Value, ct);
        return Ok(assessment);
    }

    [HttpGet("my-reviews")]
    public async Task<IActionResult> GetMyReviews(CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var assignments = await _reviewService.GetReviewerAssignmentsAsync(callerUserId.Value, ct);
        return Ok(assignments);
    }

    [HttpPost("{id:guid}/decision")]
    public async Task<IActionResult> ApplySponsorDecision([FromRoute] Guid id, [FromBody] SponsorDecisionRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var validation = await _decisionValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.ToDictionary());

        var proposal = await _conversionService.ApplySponsorDecisionAsync(id, request, callerUserId.Value, ct);
        return Ok(proposal);
    }

    [HttpPost("{id:guid}/convert")]
    public async Task<IActionResult> ConvertToProject([FromRoute] Guid id, [FromBody] ConvertProposalRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var result = await _conversionService.ConvertToProjectAsync(id, request, callerUserId.Value, ct);
        return Ok(result);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
