using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pavena.Application.Research.DTOs;
using Pavena.Application.Research.Interfaces;

namespace Pavena.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IValidator<CreateProjectRequest> _createValidator;
    private readonly IValidator<UpdateProjectRequest> _updateValidator;
    private readonly IValidator<InviteMemberRequest> _inviteValidator;
    private readonly IValidator<CreateTaskRequest> _createTaskValidator;
    private readonly IValidator<CreateMilestoneRequest> _createMilestoneValidator;

    public ProjectsController(
        IProjectService projectService,
        IValidator<CreateProjectRequest> createValidator,
        IValidator<UpdateProjectRequest> updateValidator,
        IValidator<InviteMemberRequest> inviteValidator,
        IValidator<CreateTaskRequest> createTaskValidator,
        IValidator<CreateMilestoneRequest> createMilestoneValidator)
    {
        _projectService = projectService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _inviteValidator = inviteValidator;
        _createTaskValidator = createTaskValidator;
        _createMilestoneValidator = createMilestoneValidator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(validation.ToDictionary());

        var project = await _projectService.CreateProjectAsync(callerUserId.Value, request, cancellationToken);
        return CreatedAtAction(nameof(GetProjectById), new { id = project.Id }, project);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyProjects(CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var projects = await _projectService.GetUserProjectsAsync(callerUserId.Value, cancellationToken);
        return Ok(projects);
    }

    [AllowAnonymous]
    [HttpGet("discover")]
    public async Task<IActionResult> DiscoverProjects(
        [FromQuery] string? q,
        [FromQuery] string? field,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var callerUserId = GetCurrentUserId() ?? Guid.Empty;
        var projects = await _projectService.GetDiscoverProjectsAsync(callerUserId, q, field, limit, offset, cancellationToken);
        return Ok(projects);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProjectById(Guid id, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var project = await _projectService.GetProjectByIdAsync(callerUserId.Value, id, cancellationToken);
        return Ok(project);
    }

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetProjectBySlug(string slug, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var project = await _projectService.GetProjectBySlugAsync(callerUserId.Value, slug, cancellationToken);
        return Ok(project);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(validation.ToDictionary());

        var updated = await _projectService.UpdateProjectAsync(callerUserId.Value, id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateProjectStatusRequest request, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var updated = await _projectService.UpdateProjectStatusAsync(callerUserId.Value, id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/stage")]
    public async Task<IActionResult> AdvanceStage(Guid id, [FromBody] AdvanceProjectStageRequest request, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var updated = await _projectService.AdvanceProjectStageAsync(callerUserId.Value, id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/transfer-lead")]
    public async Task<IActionResult> TransferLeadership(Guid id, [FromBody] TransferLeadershipRequest request, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        await _projectService.TransferLeadershipAsync(callerUserId.Value, id, request, cancellationToken);
        return Ok(new { message = "Project leadership transferred successfully." });
    }

    [HttpPost("{id:guid}/members/invite")]
    public async Task<IActionResult> InviteMember(Guid id, [FromBody] InviteMemberRequest request, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var validation = await _inviteValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(validation.ToDictionary());

        var invitation = await _projectService.InviteMemberAsync(callerUserId.Value, id, request, cancellationToken);
        return Ok(invitation);
    }

    [HttpGet("invitations/mine")]
    public async Task<IActionResult> GetMyInvitations(CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var invitations = await _projectService.GetMyInvitationsAsync(callerUserId.Value, cancellationToken);
        return Ok(invitations);
    }

    [HttpPost("invitations/{invitationId:guid}/respond")]
    public async Task<IActionResult> RespondInvitation(Guid invitationId, [FromBody] RespondInvitationRequest request, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        await _projectService.RespondInvitationAsync(callerUserId.Value, invitationId, request, cancellationToken);
        return Ok(new { message = request.Accept ? "Invitation accepted." : "Invitation declined." });
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        await _projectService.RemoveMemberAsync(callerUserId.Value, id, userId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/members/{userId:guid}/role")]
    public async Task<IActionResult> UpdateMemberRole(Guid id, Guid userId, [FromBody] UpdateMemberRoleRequest request, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        await _projectService.UpdateMemberRoleAsync(callerUserId.Value, id, userId, request, cancellationToken);
        return Ok(new { message = "Member role updated." });
    }

    [HttpPost("{id:guid}/tasks")]
    public async Task<IActionResult> CreateTask(Guid id, [FromBody] CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var validation = await _createTaskValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(validation.ToDictionary());

        var task = await _projectService.CreateTaskAsync(callerUserId.Value, id, request, cancellationToken);
        return CreatedAtAction(nameof(GetProjectById), new { id }, task);
    }

    [HttpPut("{id:guid}/tasks/{taskId:guid}")]
    public async Task<IActionResult> UpdateTask(Guid id, Guid taskId, [FromBody] UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var task = await _projectService.UpdateTaskAsync(callerUserId.Value, id, taskId, request, cancellationToken);
        return Ok(task);
    }

    [HttpDelete("{id:guid}/tasks/{taskId:guid}")]
    public async Task<IActionResult> DeleteTask(Guid id, Guid taskId, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        await _projectService.DeleteTaskAsync(callerUserId.Value, id, taskId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/milestones")]
    public async Task<IActionResult> CreateMilestone(Guid id, [FromBody] CreateMilestoneRequest request, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var validation = await _createMilestoneValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(validation.ToDictionary());

        var milestone = await _projectService.CreateMilestoneAsync(callerUserId.Value, id, request, cancellationToken);
        return CreatedAtAction(nameof(GetProjectById), new { id }, milestone);
    }

    [HttpPut("{id:guid}/milestones/{milestoneId:guid}")]
    public async Task<IActionResult> UpdateMilestone(Guid id, Guid milestoneId, [FromBody] UpdateMilestoneRequest request, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var milestone = await _projectService.UpdateMilestoneAsync(callerUserId.Value, id, milestoneId, request, cancellationToken);
        return Ok(milestone);
    }

    [HttpDelete("{id:guid}/milestones/{milestoneId:guid}")]
    public async Task<IActionResult> DeleteMilestone(Guid id, Guid milestoneId, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        await _projectService.DeleteMilestoneAsync(callerUserId.Value, id, milestoneId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/timeline")]
    public async Task<IActionResult> GetTimeline(Guid id, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var logs = await _projectService.GetActivityTimelineAsync(callerUserId.Value, id, cancellationToken);
        return Ok(logs);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var guid) ? guid : null;
    }
}
