using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Enums;

namespace Pavena.Domain.Entities.Research;

public class ResearchMember : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public ResearchRole Role { get; set; } = ResearchRole.Member;
    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid AddedByUserId { get; set; }

    public virtual ResearchProject? Project { get; set; }
    public virtual User? User { get; set; }
    public virtual User? AddedByUser { get; set; }

    public ResearchMember()
    {
    }

    public ResearchMember(Guid projectId, Guid userId, ResearchRole role, Guid addedByUserId)
    {
        ProjectId = projectId;
        UserId = userId;
        Role = role;
        AddedByUserId = addedByUserId;
        JoinedAtUtc = DateTime.UtcNow;
    }
}

public class ResearchTask : BaseEntity
{
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? AssigneeUserId { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public TaskStatus Status { get; set; } = TaskStatus.Todo;
    public DateTime? DueDateUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public virtual ResearchProject? Project { get; set; }
    public virtual User? AssigneeUser { get; set; }
    public virtual User? CreatedByUser { get; set; }
}
