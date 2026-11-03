namespace Pavena.Application.Research.DTOs;

public record CreateProjectRequest(
    string Title,
    string Slug,
    string? Description = null,
    string? ResearchField = null,
    string? ResearchObjectives = null,
    string Visibility = "Private",
    Guid? OwnerOrganizationId = null
);

public record UpdateProjectRequest(
    string Title,
    string? Description = null,
    string? ResearchField = null,
    string? ResearchObjectives = null,
    string Visibility = "Private"
);

public record UpdateProjectStatusRequest(
    string Status,
    string? Reason = null
);

public record AdvanceProjectStageRequest(
    string Stage
);

public record InviteMemberRequest(
    Guid InvitedUserId,
    string Role = "Researcher",
    string? Message = null
);

public record RespondInvitationRequest(
    bool Accept
);

public record UpdateMemberRoleRequest(
    string NewRole
);

public record TransferLeadershipRequest(
    Guid NewLeadUserId
);

public record CreateTaskRequest(
    string Title,
    string? Description = null,
    Guid? AssigneeUserId = null,
    string Priority = "Medium",
    DateTime? DueDateUtc = null,
    Guid? MilestoneId = null
);

public record UpdateTaskRequest(
    string Title,
    string? Description = null,
    Guid? AssigneeUserId = null,
    string Priority = "Medium",
    string Status = "Todo",
    DateTime? DueDateUtc = null,
    Guid? MilestoneId = null
);

public record CreateMilestoneRequest(
    string Title,
    DateTime TargetDateUtc,
    string? Description = null
);

public record UpdateMilestoneRequest(
    string Title,
    DateTime TargetDateUtc,
    string Status = "Planned",
    string? Description = null
);

public record ProjectSummaryDto(
    Guid Id,
    string Title,
    string Slug,
    string? Abstract,
    string? ResearchField,
    string OriginType,
    string Visibility,
    string Status,
    string Stage,
    Guid? OwnerUserId,
    string? OwnerDisplayName,
    Guid? OwnerOrganizationId,
    string? OwnerOrganizationName,
    int MemberCount,
    int TaskCount,
    int MilestoneCount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);

public record ProjectDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string? Abstract,
    string? ResearchObjectives,
    string? ResearchField,
    string OriginType,
    Guid? SourceChallengeId,
    Guid? SourceProposalId,
    string Visibility,
    string Status,
    string Stage,
    Guid? OwnerUserId,
    string? OwnerDisplayName,
    Guid? OwnerOrganizationId,
    string? OwnerOrganizationName,
    DateTime? StartDate,
    DateTime? EndDate,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<ProjectMemberDto> Members,
    IReadOnlyList<ProjectTaskDto> Tasks,
    IReadOnlyList<ProjectMilestoneDto> Milestones,
    IReadOnlyList<ProjectInvitationDto> Invitations
);

public record ProjectMemberDto(
    Guid Id,
    Guid ProjectId,
    Guid UserId,
    string DisplayName,
    string? Headline,
    string? AvatarUrl,
    string Role,
    string Status,
    DateTime? JoinedAtUtc
);

public record ProjectTaskDto(
    Guid Id,
    Guid ProjectId,
    Guid? MilestoneId,
    string? MilestoneTitle,
    string Title,
    string? Description,
    Guid? AssigneeUserId,
    string? AssigneeDisplayName,
    string Priority,
    string Status,
    DateTime? DueDateUtc,
    Guid CreatedByUserId,
    DateTime? CompletedAtUtc,
    DateTime CreatedAtUtc
);

public record ProjectMilestoneDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    string? Description,
    DateTime TargetDateUtc,
    string Status,
    DateTime? AchievedAtUtc,
    int TotalTasksCount,
    int CompletedTasksCount,
    DateTime CreatedAtUtc
);

public record ProjectInvitationDto(
    Guid Id,
    Guid ProjectId,
    string ProjectTitle,
    Guid InvitedUserId,
    string InvitedUserDisplayName,
    Guid InvitedByUserId,
    string InvitedByDisplayName,
    string RoleOffered,
    string Status,
    string? Message,
    DateTime ExpiresAtUtc,
    DateTime CreatedAtUtc
);

public record ProjectActivityLogDto(
    Guid Id,
    Guid ProjectId,
    Guid ActorUserId,
    string ActorDisplayName,
    string Action,
    string Details,
    DateTime TimestampUtc
);
