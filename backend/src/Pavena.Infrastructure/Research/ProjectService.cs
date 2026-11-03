using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pavena.Application.Common.Authorization;
using Pavena.Application.Common.Interfaces;
using Pavena.Application.Research.DTOs;
using Pavena.Application.Research.Interfaces;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Entities.Research;
using Pavena.Domain.Enums;

namespace Pavena.Infrastructure.Research;

public class ProjectService : IProjectService
{
    private readonly IPavenaDbContext _dbContext;
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        IPavenaDbContext dbContext,
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ProjectService> logger)
    {
        _dbContext = dbContext;
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<ProjectSummaryDto> CreateProjectAsync(Guid creatorUserId, CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var slug = request.Slug.ToLowerInvariant().Trim();
        var isSlugTaken = await _dbContext.ResearchProjects.AnyAsync(p => p.Slug == slug, cancellationToken);
        if (isSlugTaken)
            throw new DomainException($"A project with slug '{slug}' already exists.");

        if (!Enum.TryParse<ProjectVisibility>(request.Visibility, true, out var visibility))
            visibility = ProjectVisibility.Private;

        ResearchProject project;

        if (request.OwnerOrganizationId.HasValue)
        {
            var isOrgAdmin = await _dbContext.OrganizationMemberships
                .AsNoTracking()
                .AnyAsync(m => m.OrganizationId == request.OwnerOrganizationId.Value &&
                               m.UserId == creatorUserId &&
                               m.Role == OrgMembershipRole.OrganizationAdmin &&
                               m.IsActive, cancellationToken);

            if (!isOrgAdmin)
                throw new UnauthorizedAccessException("Only organization administrators can create institutional research projects.");

            project = ResearchProject.CreateInstitutional(
                request.Title,
                slug,
                request.OwnerOrganizationId.Value,
                creatorUserId,
                request.Description,
                request.ResearchField,
                request.ResearchObjectives);
        }
        else
        {
            project = ResearchProject.CreatePersonal(
                request.Title,
                slug,
                creatorUserId,
                request.Description,
                request.ResearchField,
                request.ResearchObjectives);
        }

        project.Visibility = visibility;

        _dbContext.ResearchProjects.Add(project);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Research project {ProjectId} ('{Slug}') created by user {UserId}", project.Id, project.Slug, creatorUserId);
        return MapToSummaryDto(project, memberCount: 1, taskCount: 0, milestoneCount: 0);
    }

    public async Task<ProjectDetailDto> GetProjectByIdAsync(Guid callerUserId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await QueryProjectDetails()
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
            throw new KeyNotFoundException("Research project not found.");

        await AuthorizeProjectAccessAsync(callerUserId, project, ResourceOperation.Read);
        return MapToDetailDto(project);
    }

    public async Task<ProjectDetailDto> GetProjectBySlugAsync(Guid callerUserId, string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.ToLowerInvariant().Trim();
        var project = await QueryProjectDetails()
            .FirstOrDefaultAsync(p => p.Slug == normalizedSlug, cancellationToken);

        if (project == null)
            throw new KeyNotFoundException("Research project not found.");

        await AuthorizeProjectAccessAsync(callerUserId, project, ResourceOperation.Read);
        return MapToDetailDto(project);
    }

    public async Task<IReadOnlyList<ProjectSummaryDto>> GetUserProjectsAsync(Guid callerUserId, CancellationToken cancellationToken = default)
    {
        var projects = await _dbContext.ResearchProjects
            .AsNoTracking()
            .Include(p => p.OwnerUser).ThenInclude(u => u!.Profile)
            .Include(p => p.OwnerOrganization)
            .Include(p => p.Members)
            .Include(p => p.Tasks)
            .Include(p => p.Milestones)
            .Where(p => p.OwnerUserId == callerUserId ||
                        p.Members.Any(m => m.UserId == callerUserId && m.Status == ProjectMembershipStatus.Active))
            .OrderByDescending(p => p.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

        return projects.Select(p => MapToSummaryDto(
            p,
            memberCount: p.Members.Count(m => m.Status == ProjectMembershipStatus.Active),
            taskCount: p.Tasks.Count,
            milestoneCount: p.Milestones.Count
        )).ToList();
    }

    public async Task<IReadOnlyList<ProjectSummaryDto>> GetDiscoverProjectsAsync(
        Guid callerUserId,
        string? q,
        string? field,
        int limit = 20,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ResearchProject> query = _dbContext.ResearchProjects
            .AsNoTracking()
            .Include(p => p.OwnerUser).ThenInclude(u => u!.Profile)
            .Include(p => p.OwnerOrganization)
            .Include(p => p.Members)
            .Include(p => p.Tasks)
            .Include(p => p.Milestones)
            .Where(p => p.Visibility == ProjectVisibility.PublicDiscovery && p.Status != ProjectStatus.Archived);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim().ToLower();
            query = query.Where(p => p.Title.ToLower().Contains(search) || (p.Abstract != null && p.Abstract.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(field))
        {
            var fieldSearch = field.Trim().ToLower();
            query = query.Where(p => p.ResearchField != null && p.ResearchField.ToLower().Contains(fieldSearch));
        }

        var results = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return results.Select(p => MapToSummaryDto(
            p,
            memberCount: p.Members.Count(m => m.Status == ProjectMembershipStatus.Active),
            taskCount: p.Tasks.Count,
            milestoneCount: p.Milestones.Count
        )).ToList();
    }

    public async Task<ProjectDetailDto> UpdateProjectAsync(Guid callerUserId, Guid projectId, UpdateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var project = await QueryProjectDetails()
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
            throw new KeyNotFoundException("Research project not found.");

        await AuthorizeProjectAccessAsync(callerUserId, project, ResourceOperation.Update);

        if (!Enum.TryParse<ProjectVisibility>(request.Visibility, true, out var visibility))
            visibility = ProjectVisibility.Private;

        project.UpdateMetadata(request.Title, request.Description, request.ResearchField, request.ResearchObjectives, visibility, callerUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDetailDto(project);
    }

    public async Task<ProjectDetailDto> UpdateProjectStatusAsync(Guid callerUserId, Guid projectId, UpdateProjectStatusRequest request, CancellationToken cancellationToken = default)
    {
        var project = await QueryProjectDetails()
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
            throw new KeyNotFoundException("Research project not found.");

        await AuthorizeProjectAccessAsync(callerUserId, project, ResourceOperation.Update);

        if (!Enum.TryParse<ProjectStatus>(request.Status, true, out var newStatus))
            throw new DomainException($"Invalid project status '{request.Status}'.");

        project.TransitionStatus(newStatus, callerUserId, request.Reason);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDetailDto(project);
    }

    public async Task<ProjectDetailDto> AdvanceProjectStageAsync(Guid callerUserId, Guid projectId, AdvanceProjectStageRequest request, CancellationToken cancellationToken = default)
    {
        var project = await QueryProjectDetails()
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
            throw new KeyNotFoundException("Research project not found.");

        await AuthorizeProjectAccessAsync(callerUserId, project, ResourceOperation.Update);

        if (!Enum.TryParse<ResearchStage>(request.Stage, true, out var newStage))
            throw new DomainException($"Invalid research stage '{request.Stage}'.");

        project.AdvanceStage(newStage, callerUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDetailDto(project);
    }

    public async Task TransferLeadershipAsync(Guid callerUserId, Guid projectId, TransferLeadershipRequest request, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.ResearchProjects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
            throw new KeyNotFoundException("Research project not found.");

        await AuthorizeProjectAccessAsync(callerUserId, project, ResourceOperation.Update);

        project.TransferLeadership(callerUserId, request.NewLeadUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProjectInvitationDto> InviteMemberAsync(Guid callerUserId, Guid projectId, InviteMemberRequest request, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.ResearchProjects
            .Include(p => p.Members)
            .Include(p => p.Invitations)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
            throw new KeyNotFoundException("Research project not found.");

        await AuthorizeProjectAccessAsync(callerUserId, project, ResourceOperation.ManageMembers);

        // Bilateral block check: ensure no active block between inviter and invitee
        var isBlocked = await _dbContext.UserBlocks
            .AsNoTracking()
            .AnyAsync(b => (b.BlockerUserId == callerUserId && b.BlockedUserId == request.InvitedUserId) ||
                           (b.BlockerUserId == request.InvitedUserId && b.BlockedUserId == callerUserId), cancellationToken);

        if (isBlocked)
            throw new DomainException("Cannot invite a user with whom a block relationship exists.");

        if (!Enum.TryParse<ProjectMemberRole>(request.Role, true, out var role))
            role = ProjectMemberRole.Researcher;

        var invitation = project.CreateInvitation(request.InvitedUserId, role, callerUserId, request.Message);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var invitedUser = await _dbContext.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == request.InvitedUserId, cancellationToken);
        var inviterUser = await _dbContext.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == callerUserId, cancellationToken);

        return new ProjectInvitationDto(
            invitation.Id,
            project.Id,
            project.Title,
            invitation.InvitedUserId,
            invitedUser?.Profile?.DisplayName ?? "Researcher",
            invitation.InvitedByUserId,
            inviterUser?.Profile?.DisplayName ?? "Project Lead",
            invitation.RoleOffered.ToString(),
            invitation.Status.ToString(),
            invitation.Message,
            invitation.ExpiresAtUtc,
            invitation.CreatedAtUtc);
    }

    public async Task<IReadOnlyList<ProjectInvitationDto>> GetMyInvitationsAsync(Guid callerUserId, CancellationToken cancellationToken = default)
    {
        var invitations = await _dbContext.ProjectInvitations
            .AsNoTracking()
            .Include(i => i.Project)
            .Include(i => i.InvitedByUser).ThenInclude(u => u!.Profile)
            .Where(i => i.InvitedUserId == callerUserId &&
                        i.Status == ProjectInvitationStatus.Pending &&
                        i.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return invitations.Select(i => new ProjectInvitationDto(
            i.Id,
            i.ProjectId,
            i.Project?.Title ?? "Untitled Project",
            i.InvitedUserId,
            "Me",
            i.InvitedByUserId,
            i.InvitedByUser?.Profile?.DisplayName ?? "Project Lead",
            i.RoleOffered.ToString(),
            i.Status.ToString(),
            i.Message,
            i.ExpiresAtUtc,
            i.CreatedAtUtc
        )).ToList();
    }

    public async Task RespondInvitationAsync(Guid callerUserId, Guid invitationId, RespondInvitationRequest request, CancellationToken cancellationToken = default)
    {
        var invitation = await _dbContext.ProjectInvitations
            .Include(i => i.Project).ThenInclude(p => p!.Members)
            .Include(i => i.Project).ThenInclude(p => p!.Invitations)
            .FirstOrDefaultAsync(i => i.Id == invitationId && i.InvitedUserId == callerUserId, cancellationToken);

        if (invitation == null || invitation.Project == null)
            throw new KeyNotFoundException("Invitation not found or has expired.");

        if (request.Accept)
        {
            invitation.Project.AcceptInvitation(invitationId, callerUserId);
        }
        else
        {
            invitation.Project.DeclineInvitation(invitationId, callerUserId);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveMemberAsync(Guid callerUserId, Guid projectId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.ResearchProjects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
            throw new KeyNotFoundException("Research project not found.");

        await AuthorizeProjectAccessAsync(callerUserId, project, ResourceOperation.ManageMembers);

        project.RemoveMember(targetUserId, callerUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateMemberRoleAsync(Guid callerUserId, Guid projectId, Guid targetUserId, UpdateMemberRoleRequest request, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.ResearchProjects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
            throw new KeyNotFoundException("Research project not found.");

        await AuthorizeProjectAccessAsync(callerUserId, project, ResourceOperation.ManageMembers);

        if (!Enum.TryParse<ProjectMemberRole>(request.NewRole, true, out var newRole))
            throw new DomainException($"Invalid member role '{request.NewRole}'.");

        project.UpdateMemberRole(targetUserId, newRole, callerUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProjectTaskDto> CreateTaskAsync(Guid callerUserId, Guid projectId, CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.ResearchProjects
            .Include(p => p.Members)
            .Include(p => p.Tasks)
            .Include(p => p.Milestones)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
            throw new KeyNotFoundException("Research project not found.");

        await AuthorizeProjectAccessAsync(callerUserId, project, ResourceOperation.Read);
        if (!project.IsActiveMember(callerUserId))
            throw new UnauthorizedAccessException("Only active members can add tasks to this project.");

        if (!Enum.TryParse<TaskPriority>(request.Priority, true, out var priority))
            priority = TaskPriority.Medium;

        var task = project.AddTask(
            request.Title,
            callerUserId,
            request.Description,
            request.AssigneeUserId,
            priority,
            request.DueDateUtc,
            request.MilestoneId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        string? milestoneTitle = null;
        if (task.MilestoneId.HasValue)
        {
            var ms = await _dbContext.ResearchMilestones.FindAsync(new object[] { task.MilestoneId.Value }, cancellationToken);
            milestoneTitle = ms?.Title;
        }

        string? assigneeName = null;
        if (task.AssigneeUserId.HasValue)
        {
            var u = await _dbContext.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == task.AssigneeUserId.Value, cancellationToken);
            assigneeName = u?.Profile?.DisplayName;
        }

        return new ProjectTaskDto(
            task.Id,
            task.ProjectId,
            task.MilestoneId,
            milestoneTitle,
            task.Title,
            task.Description,
            task.AssigneeUserId,
            assigneeName,
            task.Priority.ToString(),
            task.Status.ToString(),
            task.DueDateUtc,
            task.CreatedByUserId,
            task.CompletedAtUtc,
            task.CreatedAtUtc);
    }

    public async Task<ProjectTaskDto> UpdateTaskAsync(Guid callerUserId, Guid projectId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.ResearchProjects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
            throw new KeyNotFoundException("Research project not found.");

        await AuthorizeProjectAccessAsync(callerUserId, project, ResourceOperation.Read);
        if (!project.IsActiveMember(callerUserId))
            throw new UnauthorizedAccessException("Only active members can update tasks.");

        var task = await _dbContext.ResearchTasks
            .Include(t => t.Milestone)
            .Include(t => t.AssigneeUser).ThenInclude(u => u!.Profile)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId, cancellationToken);

        if (task == null)
            throw new KeyNotFoundException("Task not found.");

        if (!Enum.TryParse<TaskPriority>(request.Priority, true, out var priority))
            priority = TaskPriority.Medium;

        if (!Enum.TryParse<ResearchTaskStatus>(request.Status, true, out var status))
            status = ResearchTaskStatus.Todo;

        if (request.AssigneeUserId.HasValue && request.AssigneeUserId.Value != Guid.Empty)
        {
            var isAssigneeMember = project.Members.Any(m => m.UserId == request.AssigneeUserId.Value && m.Status == ProjectMembershipStatus.Active);
            if (!isAssigneeMember)
                throw new DomainException("Task assignee must be an active project member.");
        }

        var prevStatus = task.Status;
        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();
        task.AssigneeUserId = request.AssigneeUserId;
        task.Priority = priority;
        task.Status = status;
        task.DueDateUtc = request.DueDateUtc;
        task.MilestoneId = request.MilestoneId;

        if (prevStatus != ResearchTaskStatus.Done && status == ResearchTaskStatus.Done)
        {
            task.CompletedAtUtc = DateTime.UtcNow;
            project.LogActivity(callerUserId, "TaskCompleted", $"Task '{task.Title}' marked as completed.");
        }
        else if (status != ResearchTaskStatus.Done)
        {
            task.CompletedAtUtc = null;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProjectTaskDto(
            task.Id,
            task.ProjectId,
            task.MilestoneId,
            task.Milestone?.Title,
            task.Title,
            task.Description,
            task.AssigneeUserId,
            task.AssigneeUser?.Profile?.DisplayName,
            task.Priority.ToString(),
            task.Status.ToString(),
            task.DueDateUtc,
            task.CreatedByUserId,
            task.CompletedAtUtc,
            task.CreatedAtUtc);
    }

    public async Task DeleteTaskAsync(Guid callerUserId, Guid projectId, Guid taskId, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.ResearchProjects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
            throw new KeyNotFoundException("Research project not found.");

        await AuthorizeProjectAccessAsync(callerUserId, project, ResourceOperation.Read);
        if (!project.IsLeadOrOwner(callerUserId))
            throw new UnauthorizedAccessException("Only the project lead can delete tasks.");

        var task = await _dbContext.ResearchTasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId, cancellationToken);

        if (task == null)
            throw new KeyNotFoundException("Task not found.");

        _dbContext.ResearchTasks.Remove(task);
        project.LogActivity(callerUserId, "TaskDeleted", $"Task '{task.Title}' was removed.");
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProjectMilestoneDto> CreateMilestoneAsync(Guid callerUserId, Guid projectId, CreateMilestoneRequest request, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.ResearchProjects
            .Include(p => p.Members)
            .Include(p => p.Milestones)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
            throw new KeyNotFoundException("Research project not found.");

        await AuthorizeProjectAccessAsync(callerUserId, project, ResourceOperation.Update);

        var milestone = project.AddMilestone(request.Title, request.TargetDateUtc, callerUserId, request.Description);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProjectMilestoneDto(
            milestone.Id,
            milestone.ProjectId,
            milestone.Title,
            milestone.Description,
            milestone.TargetDateUtc,
            milestone.Status.ToString(),
            milestone.AchievedAtUtc,
            0,
            0,
            milestone.CreatedAtUtc);
    }

    public async Task<ProjectMilestoneDto> UpdateMilestoneAsync(Guid callerUserId, Guid projectId, Guid milestoneId, UpdateMilestoneRequest request, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.ResearchProjects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
            throw new KeyNotFoundException("Research project not found.");

        await AuthorizeProjectAccessAsync(callerUserId, project, ResourceOperation.Update);

        var milestone = await _dbContext.ResearchMilestones
            .Include(m => m.Tasks)
            .FirstOrDefaultAsync(m => m.Id == milestoneId && m.ProjectId == projectId, cancellationToken);

        if (milestone == null)
            throw new KeyNotFoundException("Milestone not found.");

        if (!Enum.TryParse<MilestoneStatus>(request.Status, true, out var status))
            status = MilestoneStatus.Planned;

        var prevStatus = milestone.Status;
        milestone.Title = request.Title.Trim();
        milestone.Description = request.Description?.Trim();
        milestone.TargetDateUtc = request.TargetDateUtc;
        milestone.Status = status;

        if (prevStatus != MilestoneStatus.Achieved && status == MilestoneStatus.Achieved)
        {
            milestone.AchievedAtUtc = DateTime.UtcNow;
            project.LogActivity(callerUserId, "MilestoneAchieved", $"Milestone '{milestone.Title}' reached and marked achieved.");
        }
        else if (status != MilestoneStatus.Achieved)
        {
            milestone.AchievedAtUtc = null;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProjectMilestoneDto(
            milestone.Id,
            milestone.ProjectId,
            milestone.Title,
            milestone.Description,
            milestone.TargetDateUtc,
            milestone.Status.ToString(),
            milestone.AchievedAtUtc,
            milestone.Tasks.Count,
            milestone.Tasks.Count(t => t.Status == ResearchTaskStatus.Done),
            milestone.CreatedAtUtc);
    }

    public async Task DeleteMilestoneAsync(Guid callerUserId, Guid projectId, Guid milestoneId, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.ResearchProjects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
            throw new KeyNotFoundException("Research project not found.");

        await AuthorizeProjectAccessAsync(callerUserId, project, ResourceOperation.Update);

        var milestone = await _dbContext.ResearchMilestones
            .FirstOrDefaultAsync(m => m.Id == milestoneId && m.ProjectId == projectId, cancellationToken);

        if (milestone == null)
            throw new KeyNotFoundException("Milestone not found.");

        _dbContext.ResearchMilestones.Remove(milestone);
        project.LogActivity(callerUserId, "MilestoneDeleted", $"Milestone '{milestone.Title}' deleted.");
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectActivityLogDto>> GetActivityTimelineAsync(Guid callerUserId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.ResearchProjects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
            throw new KeyNotFoundException("Research project not found.");

        await AuthorizeProjectAccessAsync(callerUserId, project, ResourceOperation.Read);

        var logs = await _dbContext.ProjectActivityLogs
            .AsNoTracking()
            .Include(a => a.ActorUser).ThenInclude(u => u!.Profile)
            .Where(a => a.ProjectId == projectId)
            .OrderByDescending(a => a.TimestampUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        return logs.Select(a => new ProjectActivityLogDto(
            a.Id,
            a.ProjectId,
            a.ActorUserId,
            a.ActorUser?.Profile?.DisplayName ?? "Researcher",
            a.Action,
            a.Details,
            a.TimestampUtc
        )).ToList();
    }

    private IQueryable<ResearchProject> QueryProjectDetails()
    {
        return _dbContext.ResearchProjects
            .Include(p => p.OwnerUser).ThenInclude(u => u!.Profile)
            .Include(p => p.OwnerOrganization)
            .Include(p => p.Members).ThenInclude(m => m.User).ThenInclude(u => u!.Profile)
            .Include(p => p.Tasks).ThenInclude(t => t.Milestone)
            .Include(p => p.Tasks).ThenInclude(t => t.AssigneeUser).ThenInclude(u => u!.Profile)
            .Include(p => p.Milestones).ThenInclude(m => m.Tasks)
            .Include(p => p.Invitations).ThenInclude(i => i.InvitedUser).ThenInclude(u => u!.Profile)
            .Include(p => p.Invitations).ThenInclude(i => i.InvitedByUser).ThenInclude(u => u!.Profile);
    }

    private async Task AuthorizeProjectAccessAsync(Guid callerUserId, ResearchProject project, ResourceOperation operation)
    {
        var principal = GetUserPrincipal(callerUserId);
        var result = await _authorizationService.AuthorizeAsync(principal, project, new ResourceOperationRequirement(operation));
        if (!result.Succeeded)
        {
            // Mask private projects with KeyNotFoundException (404) to prevent enumeration
            throw new KeyNotFoundException("Research project not found.");
        }
    }

    private ClaimsPrincipal GetUserPrincipal(Guid userId)
    {
        var httpUser = _httpContextAccessor.HttpContext?.User;
        if (httpUser?.Identity?.IsAuthenticated == true)
            return httpUser;

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "TestAuth");

        return new ClaimsPrincipal(identity);
    }

    private static ProjectSummaryDto MapToSummaryDto(ResearchProject p, int memberCount, int taskCount, int milestoneCount) => new(
        p.Id,
        p.Title,
        p.Slug,
        p.Abstract,
        p.ResearchField,
        p.OriginType.ToString(),
        p.Visibility.ToString(),
        p.Status.ToString(),
        p.Stage.ToString(),
        p.OwnerUserId,
        p.OwnerUser?.Profile?.DisplayName,
        p.OwnerOrganizationId,
        p.OwnerOrganization?.Name,
        memberCount,
        taskCount,
        milestoneCount,
        p.CreatedAtUtc,
        p.UpdatedAtUtc
    );

    private static ProjectDetailDto MapToDetailDto(ResearchProject p) => new(
        p.Id,
        p.Title,
        p.Slug,
        p.Abstract,
        p.ResearchObjectives,
        p.ResearchField,
        p.OriginType.ToString(),
        p.SourceChallengeId,
        p.SourceProposalId,
        p.Visibility.ToString(),
        p.Status.ToString(),
        p.Stage.ToString(),
        p.OwnerUserId,
        p.OwnerUser?.Profile?.DisplayName,
        p.OwnerOrganizationId,
        p.OwnerOrganization?.Name,
        p.StartDate,
        p.EndDate,
        p.CreatedAtUtc,
        p.UpdatedAtUtc,
        p.Members.Where(m => m.Status == ProjectMembershipStatus.Active).Select(m => new ProjectMemberDto(
            m.Id,
            m.ProjectId,
            m.UserId,
            m.User?.Profile?.DisplayName ?? "Researcher",
            m.User?.Profile?.Headline,
            m.User?.Profile?.AvatarUrl,
            m.Role.ToString(),
            m.Status.ToString(),
            m.JoinedAtUtc
        )).ToList(),
        p.Tasks.Select(t => new ProjectTaskDto(
            t.Id,
            t.ProjectId,
            t.MilestoneId,
            t.Milestone?.Title,
            t.Title,
            t.Description,
            t.AssigneeUserId,
            t.AssigneeUser?.Profile?.DisplayName,
            t.Priority.ToString(),
            t.Status.ToString(),
            t.DueDateUtc,
            t.CreatedByUserId,
            t.CompletedAtUtc,
            t.CreatedAtUtc
        )).ToList(),
        p.Milestones.Select(m => new ProjectMilestoneDto(
            m.Id,
            m.ProjectId,
            m.Title,
            m.Description,
            m.TargetDateUtc,
            m.Status.ToString(),
            m.AchievedAtUtc,
            m.Tasks.Count,
            m.Tasks.Count(t => t.Status == ResearchTaskStatus.Done),
            m.CreatedAtUtc
        )).ToList(),
        p.Invitations.Where(i => i.Status == ProjectInvitationStatus.Pending && i.ExpiresAtUtc > DateTime.UtcNow).Select(i => new ProjectInvitationDto(
            i.Id,
            i.ProjectId,
            p.Title,
            i.InvitedUserId,
            i.InvitedUser?.Profile?.DisplayName ?? "Researcher",
            i.InvitedByUserId,
            i.InvitedByUser?.Profile?.DisplayName ?? "Project Lead",
            i.RoleOffered.ToString(),
            i.Status.ToString(),
            i.Message,
            i.ExpiresAtUtc,
            i.CreatedAtUtc
        )).ToList()
    );
}
