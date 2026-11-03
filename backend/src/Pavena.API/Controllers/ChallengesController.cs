using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pavena.Application.Challenges.DTOs;
using Pavena.Application.Challenges.Interfaces;
using Pavena.Domain.Enums;

namespace Pavena.API.Controllers;

[ApiController]
[Route("api/v1/challenges")]
public class ChallengesController : ControllerBase
{
    private readonly IChallengeService _challengeService;
    private readonly IChallengeAttachmentService _attachmentService;
    private readonly IChallengeNdaService _ndaService;
    private readonly IValidator<CreateChallengeRequest> _createValidator;
    private readonly IValidator<UpdateChallengeRequest> _updateValidator;
    private readonly IValidator<CancelChallengeRequest> _cancelValidator;
    private readonly IValidator<SignNdaRequest> _signNdaValidator;

    public ChallengesController(
        IChallengeService challengeService,
        IChallengeAttachmentService attachmentService,
        IChallengeNdaService ndaService,
        IValidator<CreateChallengeRequest> createValidator,
        IValidator<UpdateChallengeRequest> updateValidator,
        IValidator<CancelChallengeRequest> cancelValidator,
        IValidator<SignNdaRequest> signNdaValidator)
    {
        _challengeService = challengeService;
        _attachmentService = attachmentService;
        _ndaService = ndaService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _cancelValidator = cancelValidator;
        _signNdaValidator = signNdaValidator;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> SearchChallenges(
        [FromQuery] string? query,
        [FromQuery] string? domain,
        [FromQuery] ChallengeStatus? status,
        [FromQuery] InstitutionalEligibility? eligibility,
        [FromQuery] decimal? maxBudget,
        [FromQuery] Guid? organizationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var callerUserId = GetCurrentUserId();
        var results = await _challengeService.SearchChallengesAsync(
            callerUserId, query, domain, status, eligibility, maxBudget, organizationId, page, pageSize, ct);

        return Ok(results);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateChallenge([FromBody] CreateChallengeRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.ToDictionary());

        var challenge = await _challengeService.CreateChallengeAsync(callerUserId.Value, request, ct);
        return CreatedAtAction(nameof(GetChallengeById), new { id = challenge.Id }, challenge);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetChallengeById([FromRoute] string id, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();

        if (Guid.TryParse(id, out var challengeId))
        {
            var detail = await _challengeService.GetChallengeByIdAsync(callerUserId, challengeId, ct);
            return Ok(detail);
        }
        else
        {
            var detail = await _challengeService.GetChallengeBySlugAsync(callerUserId, id, ct);
            return Ok(detail);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateChallenge([FromRoute] Guid id, [FromBody] UpdateChallengeRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.ToDictionary());

        var updated = await _challengeService.UpdateChallengeAsync(callerUserId.Value, id, request, ct);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteDraftChallenge([FromRoute] Guid id, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        await _challengeService.DeleteDraftChallengeAsync(callerUserId.Value, id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize]
    public async Task<IActionResult> SubmitForApproval([FromRoute] Guid id, [FromBody] string? notes, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        await _challengeService.SubmitForApprovalAsync(callerUserId.Value, id, notes, ct);
        return Ok(new { success = true, message = "Challenge submitted for approval." });
    }

    [HttpPost("{id:guid}/decision")]
    [Authorize]
    public async Task<IActionResult> ProcessApprovalDecision([FromRoute] Guid id, [FromBody] ChallengeDecisionRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        await _challengeService.ProcessApprovalDecisionAsync(callerUserId.Value, id, request, ct);
        return Ok(new { success = true, decision = request.Decision.ToString() });
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize]
    public async Task<IActionResult> PublishChallenge([FromRoute] Guid id, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        await _challengeService.PublishChallengeAsync(callerUserId.Value, id, ct);
        return Ok(new { success = true, message = "Challenge published." });
    }

    [HttpPost("{id:guid}/open-proposals")]
    [Authorize]
    public async Task<IActionResult> OpenForProposals([FromRoute] Guid id, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        await _challengeService.OpenForProposalsAsync(callerUserId.Value, id, ct);
        return Ok(new { success = true, message = "Challenge is open for proposals." });
    }

    [HttpPost("{id:guid}/close")]
    [Authorize]
    public async Task<IActionResult> CloseChallenge([FromRoute] Guid id, [FromBody] string? notes, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        await _challengeService.CloseChallengeAsync(callerUserId.Value, id, notes, ct);
        return Ok(new { success = true, message = "Challenge closed." });
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize]
    public async Task<IActionResult> CancelChallenge([FromRoute] Guid id, [FromBody] CancelChallengeRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var validation = await _cancelValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.ToDictionary());

        await _challengeService.CancelChallengeAsync(callerUserId.Value, id, request.Reason, ct);
        return Ok(new { success = true, message = "Challenge cancelled." });
    }

    // Attachments
    [HttpPost("{id:guid}/attachments")]
    [Authorize]
    public async Task<IActionResult> UploadAttachment(
        [FromRoute] Guid id,
        [FromForm] IFormFile file,
        [FromForm] AttachmentConfidentialityLevel confidentiality,
        [FromForm] string? description,
        CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest("A valid file must be uploaded.");

        using var stream = file.OpenReadStream();
        var attachment = await _attachmentService.UploadAttachmentAsync(
            callerUserId.Value, id, file.FileName, stream, file.ContentType, confidentiality, description, ct);

        return Ok(attachment);
    }

    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteAttachment([FromRoute] Guid id, [FromRoute] Guid attachmentId, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        await _attachmentService.DeleteAttachmentAsync(callerUserId.Value, id, attachmentId, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/attachments/{attachmentId:guid}/download")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadAttachment([FromRoute] Guid id, [FromRoute] Guid attachmentId, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        var stream = await _attachmentService.DownloadAttachmentStreamAsync(callerUserId, id, attachmentId, ct);
        return File(stream, "application/octet-stream");
    }

    // NDA Gating
    [HttpGet("{id:guid}/nda/status")]
    [Authorize]
    public async Task<IActionResult> GetNdaStatus([FromRoute] Guid id, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var status = await _ndaService.GetNdaStatusAsync(callerUserId.Value, id, ct);
        return Ok(status);
    }

    [HttpPost("{id:guid}/nda/sign")]
    [Authorize]
    public async Task<IActionResult> SignNda([FromRoute] Guid id, [FromBody] SignNdaRequest request, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var validation = await _signNdaValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.ToDictionary());

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var status = await _ndaService.SignNdaAsync(callerUserId.Value, id, request, ip, ct);
        return Ok(status);
    }

    [HttpPost("{id:guid}/nda/{ndaId:guid}/approve")]
    [Authorize]
    public async Task<IActionResult> ApproveNda([FromRoute] Guid id, [FromRoute] Guid ndaId, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        await _ndaService.ApproveNdaAsync(callerUserId.Value, id, ndaId, ct);
        return Ok(new { success = true, message = "NDA agreement approved." });
    }

    // Eligibility & Activity
    [HttpGet("{id:guid}/eligibility")]
    [Authorize]
    public async Task<IActionResult> CheckEligibility([FromRoute] Guid id, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var result = await _challengeService.CheckEligibilityAsync(callerUserId.Value, id, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}/activity")]
    [Authorize]
    public async Task<IActionResult> GetActivityLogs([FromRoute] Guid id, CancellationToken ct)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue) return Unauthorized();

        var logs = await _challengeService.GetActivityLogsAsync(callerUserId.Value, id, ct);
        return Ok(logs);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
