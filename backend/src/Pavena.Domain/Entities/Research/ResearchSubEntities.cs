using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Enums;

namespace Pavena.Domain.Entities.Research;

public class ResearchMember : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public ProjectMemberRole Role { get; set; } = ProjectMemberRole.Researcher;
    public ProjectMembershipStatus Status { get; set; } = ProjectMembershipStatus.Active;
    public DateTime? InvitedAtUtc { get; set; }
    public DateTime? JoinedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAtUtc { get; set; }
    public Guid AddedByUserId { get; set; }

    public virtual ResearchProject? Project { get; set; }
    public virtual User? User { get; set; }
    public virtual User? AddedByUser { get; set; }

    public ResearchMember()
    {
    }

    public ResearchMember(Guid projectId, Guid userId, ProjectMemberRole role, Guid addedByUserId)
    {
        ProjectId = projectId;
        UserId = userId;
        Role = role;
        Status = ProjectMembershipStatus.Active;
        AddedByUserId = addedByUserId;
        JoinedAtUtc = DateTime.UtcNow;
    }

    public ResearchMember(Guid projectId, Guid userId, ResearchRole role, Guid addedByUserId)
        : this(projectId, userId, MapLegacyRole(role), addedByUserId)
    {
    }

    private static ProjectMemberRole MapLegacyRole(ResearchRole legacyRole) => legacyRole switch
    {
        ResearchRole.Owner => ProjectMemberRole.ProjectLead,
        ResearchRole.ProjectManager => ProjectMemberRole.ProjectLead,
        ResearchRole.Supervisor => ProjectMemberRole.CoResearcher,
        ResearchRole.Member => ProjectMemberRole.Researcher,
        ResearchRole.Reviewer => ProjectMemberRole.Observer,
        ResearchRole.Observer => ProjectMemberRole.Observer,
        _ => ProjectMemberRole.Researcher
    };
}

public class ResearchMilestone : BaseEntity
{
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime TargetDateUtc { get; set; }
    public MilestoneStatus Status { get; set; } = MilestoneStatus.Planned;
    public Guid CreatedByUserId { get; set; }
    public DateTime? AchievedAtUtc { get; set; }

    public virtual ResearchProject? Project { get; set; }
    public virtual User? CreatedByUser { get; set; }
    public virtual ICollection<ResearchTask> Tasks { get; set; } = new List<ResearchTask>();

    public ResearchMilestone()
    {
    }

    public ResearchMilestone(Guid projectId, string title, string? description, DateTime targetDateUtc, Guid createdByUserId)
    {
        ProjectId = projectId;
        Title = title.Trim();
        Description = description?.Trim();
        TargetDateUtc = targetDateUtc;
        CreatedByUserId = createdByUserId;
        Status = MilestoneStatus.Planned;
    }
}

public class ResearchTask : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid? MilestoneId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? AssigneeUserId { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public ResearchTaskStatus Status { get; set; } = ResearchTaskStatus.Todo;
    public DateTime? DueDateUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public virtual ResearchProject? Project { get; set; }
    public virtual ResearchMilestone? Milestone { get; set; }
    public virtual User? AssigneeUser { get; set; }
    public virtual User? CreatedByUser { get; set; }

    public ResearchTask()
    {
    }

    public ResearchTask(Guid projectId, string title, Guid createdByUserId, string? description = null, Guid? assigneeUserId = null, TaskPriority priority = TaskPriority.Medium, DateTime? dueDateUtc = null, Guid? milestoneId = null)
    {
        ProjectId = projectId;
        Title = title.Trim();
        Description = description?.Trim();
        CreatedByUserId = createdByUserId;
        AssigneeUserId = assigneeUserId;
        Priority = priority;
        DueDateUtc = dueDateUtc;
        MilestoneId = milestoneId;
        Status = ResearchTaskStatus.Todo;
    }
}

public class ProjectInvitation : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid InvitedUserId { get; set; }
    public Guid InvitedByUserId { get; set; }
    public ProjectMemberRole RoleOffered { get; set; } = ProjectMemberRole.Researcher;
    public ProjectInvitationStatus Status { get; set; } = ProjectInvitationStatus.Pending;
    public string? Message { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }

    public virtual ResearchProject? Project { get; set; }
    public virtual User? InvitedUser { get; set; }
    public virtual User? InvitedByUser { get; set; }

    public ProjectInvitation()
    {
    }

    public ProjectInvitation(Guid projectId, Guid invitedUserId, Guid invitedByUserId, ProjectMemberRole roleOffered, string? message = null, TimeSpan? validity = null)
    {
        ProjectId = projectId;
        InvitedUserId = invitedUserId;
        InvitedByUserId = invitedByUserId;
        RoleOffered = roleOffered;
        Message = message?.Trim();
        Status = ProjectInvitationStatus.Pending;
        ExpiresAtUtc = DateTime.UtcNow.Add(validity ?? TimeSpan.FromDays(14));
    }
}

public class ProjectActivityLog : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public virtual ResearchProject? Project { get; set; }
    public virtual User? ActorUser { get; set; }

    public ProjectActivityLog()
    {
    }

    public ProjectActivityLog(Guid projectId, Guid actorUserId, string action, string details)
    {
        ProjectId = projectId;
        ActorUserId = actorUserId;
        Action = action;
        Details = details;
        TimestampUtc = DateTime.UtcNow;
    }
}
