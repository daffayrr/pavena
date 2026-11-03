using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Entities.Organizations;
using Pavena.Domain.Enums;

namespace Pavena.Domain.Entities.Research;

public class ResearchProject : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Abstract { get; set; }
    public string? ResearchObjectives { get; set; }
    public string? ResearchField { get; set; }

    public ProjectOriginType OriginType { get; set; } = ProjectOriginType.ResearcherInitiated;
    public Guid? SourceChallengeId { get; set; }
    public Guid? SourceProposalId { get; set; }

    public Guid? OwnerUserId { get; set; }
    public Guid? OwnerOrganizationId { get; set; }

    public ProjectVisibility Visibility { get; set; } = ProjectVisibility.Private;
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    public ResearchStage Stage { get; set; } = ResearchStage.Concept;

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    public virtual User? OwnerUser { get; set; }
    public virtual Organization? OwnerOrganization { get; set; }
    public virtual ICollection<ResearchMember> Members { get; set; } = new List<ResearchMember>();
    public virtual ICollection<ResearchTask> Tasks { get; set; } = new List<ResearchTask>();
    public virtual ICollection<ResearchMilestone> Milestones { get; set; } = new List<ResearchMilestone>();
    public virtual ICollection<ProjectInvitation> Invitations { get; set; } = new List<ProjectInvitation>();
    public virtual ICollection<ProjectActivityLog> ActivityLogs { get; set; } = new List<ProjectActivityLog>();

    public ResearchProject()
    {
    }

    public static ResearchProject CreatePersonal(
        string title,
        string slug,
        Guid ownerUserId,
        string? description = null,
        string? researchField = null,
        string? objectives = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Project title is required.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Project slug is required.");

        if (ownerUserId == Guid.Empty)
            throw new DomainException("Project owner user ID is required.");

        var project = new ResearchProject
        {
            Title = title.Trim(),
            Slug = slug.ToLowerInvariant().Trim(),
            Abstract = description?.Trim(),
            ResearchObjectives = objectives?.Trim(),
            ResearchField = researchField?.Trim(),
            OriginType = ProjectOriginType.ResearcherInitiated,
            OwnerUserId = ownerUserId,
            OwnerOrganizationId = null,
            Visibility = ProjectVisibility.Private,
            Status = ProjectStatus.Active,
            Stage = ResearchStage.Concept,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        project.ValidateOwnership();

        // Automatically assign owner as ProjectLead member
        var leadMember = new ResearchMember(project.Id, ownerUserId, ProjectMemberRole.ProjectLead, ownerUserId);
        project.Members.Add(leadMember);
        project.LogActivity(ownerUserId, "ProjectCreated", $"Researcher initiated project '{project.Title}' created.");

        return project;
    }

    public static ResearchProject CreateInstitutional(
        string title,
        string slug,
        Guid ownerOrganizationId,
        Guid creatorUserId,
        string? description = null,
        string? researchField = null,
        string? objectives = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Project title is required.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Project slug is required.");

        if (ownerOrganizationId == Guid.Empty)
            throw new DomainException("Project owner organization ID is required.");

        var project = new ResearchProject
        {
            Title = title.Trim(),
            Slug = slug.ToLowerInvariant().Trim(),
            Abstract = description?.Trim(),
            ResearchObjectives = objectives?.Trim(),
            ResearchField = researchField?.Trim(),
            OriginType = ProjectOriginType.InstitutionalInitiated,
            OwnerUserId = null,
            OwnerOrganizationId = ownerOrganizationId,
            Visibility = ProjectVisibility.Private,
            Status = ProjectStatus.Active,
            Stage = ResearchStage.Concept,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        project.ValidateOwnership();

        if (creatorUserId != Guid.Empty)
        {
            var leadMember = new ResearchMember(project.Id, creatorUserId, ProjectMemberRole.InstitutionalCoordinator, creatorUserId);
            project.Members.Add(leadMember);
            project.LogActivity(creatorUserId, "ProjectCreated", $"Institution-sponsored project '{project.Title}' created.");
        }

        return project;
    }

    public static ResearchProject CreateInstitutional(string title, string slug, Guid ownerOrganizationId)
    {
        return CreateInstitutional(title, slug, ownerOrganizationId, Guid.Empty);
    }

    public static ResearchProject CreateFromProposal(
        string title,
        string slug,
        Guid leadUserId,
        Guid? sourceChallengeId,
        Guid sourceProposalId,
        string? description = null,
        string? researchField = null,
        string? objectives = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Project title is required.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Project slug is required.");

        if (leadUserId == Guid.Empty)
            throw new DomainException("Lead researcher user ID is required.");

        var project = new ResearchProject
        {
            Title = title.Trim(),
            Slug = slug.ToLowerInvariant().Trim(),
            Abstract = description?.Trim(),
            ResearchObjectives = objectives?.Trim(),
            ResearchField = researchField?.Trim(),
            OriginType = ProjectOriginType.IndustryChallenge,
            SourceChallengeId = sourceChallengeId,
            SourceProposalId = sourceProposalId,
            OwnerUserId = leadUserId,
            OwnerOrganizationId = null,
            Visibility = ProjectVisibility.Private,
            Status = ProjectStatus.Planning,
            Stage = ResearchStage.MethodologyDesign,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        project.ValidateOwnership();

        var leadMember = new ResearchMember(project.Id, leadUserId, ProjectMemberRole.ProjectLead, leadUserId);
        project.Members.Add(leadMember);
        project.LogActivity(leadUserId, "ProjectCreatedFromProposal", $"Project converted from accepted proposal for challenge {sourceChallengeId}.");

        return project;
    }

    public void UpdateMetadata(string title, string? description, string? field, string? objectives, ProjectVisibility visibility, Guid actorUserId)
    {
        EnsureLeadOrOwner(actorUserId);

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Project title cannot be empty.");

        Title = title.Trim();
        Abstract = description?.Trim();
        ResearchField = field?.Trim();
        ResearchObjectives = objectives?.Trim();
        Visibility = visibility;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();

        LogActivity(actorUserId, "ProjectUpdated", "Project metadata updated.");
    }

    public void TransitionStatus(ProjectStatus newStatus, Guid actorUserId, string? reason = null)
    {
        EnsureLeadOrOwner(actorUserId);

        if (Status == ProjectStatus.Archived && newStatus != ProjectStatus.Archived)
            throw new DomainException("Cannot reactivate an archived project directly.");

        var previous = Status;
        Status = newStatus;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();

        LogActivity(actorUserId, "StatusChanged", $"Status transitioned from {previous} to {newStatus}. Reason: {reason ?? "None"}");
    }

    public void AdvanceStage(ResearchStage newStage, Guid actorUserId)
    {
        EnsureLeadOrOwner(actorUserId);

        var previous = Stage;
        Stage = newStage;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();

        LogActivity(actorUserId, "StageAdvanced", $"Research maturity stage advanced from {previous} to {newStage}.");
    }

    public void TransferLeadership(Guid currentLeadUserId, Guid newLeadUserId)
    {
        EnsureLeadOrOwner(currentLeadUserId);

        if (currentLeadUserId == newLeadUserId)
            throw new DomainException("Cannot transfer leadership to the same user.");

        var targetMember = Members.FirstOrDefault(m => m.UserId == newLeadUserId && m.Status == ProjectMembershipStatus.Active)
            ?? throw new DomainException("New project lead must be an active project member.");

        var currentLeadMember = Members.FirstOrDefault(m => m.UserId == currentLeadUserId && m.Status == ProjectMembershipStatus.Active);
        if (currentLeadMember != null)
        {
            currentLeadMember.Role = ProjectMemberRole.CoResearcher;
        }

        targetMember.Role = ProjectMemberRole.ProjectLead;

        // If user-owned project, update OwnerUserId
        if (OwnerUserId.HasValue)
        {
            OwnerUserId = newLeadUserId;
        }

        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();

        LogActivity(currentLeadUserId, "LeadershipTransferred", $"Project leadership transferred from {currentLeadUserId} to {newLeadUserId}.");
    }

    public ResearchMember AddMember(Guid userId, ProjectMemberRole role, Guid actorUserId)
    {
        EnsureLeadOrOwner(actorUserId);
        return AddMemberInternal(userId, role, actorUserId);
    }

    private ResearchMember AddMemberInternal(Guid userId, ProjectMemberRole role, Guid addedByUserId)
    {
        if (userId == Guid.Empty)
            throw new DomainException("Invalid user ID for project member.");

        var existing = Members.FirstOrDefault(m => m.UserId == userId);
        if (existing != null)
        {
            if (existing.Status == ProjectMembershipStatus.Active)
                throw new DomainException("User is already an active member of this project.");

            existing.Status = ProjectMembershipStatus.Active;
            existing.Role = role;
            existing.JoinedAtUtc = DateTime.UtcNow;
            existing.LeftAtUtc = null;
            existing.AddedByUserId = addedByUserId;

            UpdatedAtUtc = DateTime.UtcNow;
            ConcurrencyStamp = Guid.NewGuid().ToString();
            LogActivity(addedByUserId, "MemberReactivated", $"User {userId} reactivated as {role}.");
            return existing;
        }

        var newMember = new ResearchMember(Id, userId, role, addedByUserId);
        Members.Add(newMember);

        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();
        LogActivity(addedByUserId, "MemberAdded", $"User {userId} added as {role}.");
        return newMember;
    }

    public void RemoveMember(Guid targetUserId, Guid actorUserId)
    {
        EnsureLeadOrOwner(actorUserId);

        var member = Members.FirstOrDefault(m => m.UserId == targetUserId && m.Status == ProjectMembershipStatus.Active)
            ?? throw new DomainException("Active member not found on this project.");

        if (member.Role == ProjectMemberRole.ProjectLead && OwnerUserId == targetUserId)
            throw new DomainException("Cannot remove the primary project owner/lead without transferring leadership first.");

        member.Status = ProjectMembershipStatus.Removed;
        member.LeftAtUtc = DateTime.UtcNow;

        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();
        LogActivity(actorUserId, "MemberRemoved", $"Member {targetUserId} removed from project.");
    }

    public void UpdateMemberRole(Guid targetUserId, ProjectMemberRole newRole, Guid actorUserId)
    {
        EnsureLeadOrOwner(actorUserId);

        var member = Members.FirstOrDefault(m => m.UserId == targetUserId && m.Status == ProjectMembershipStatus.Active)
            ?? throw new DomainException("Active member not found on this project.");

        if (member.Role == ProjectMemberRole.ProjectLead && newRole != ProjectMemberRole.ProjectLead)
            throw new DomainException("Use TransferLeadership to change ProjectLead role.");

        var oldRole = member.Role;
        member.Role = newRole;

        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();
        LogActivity(actorUserId, "MemberRoleUpdated", $"Member {targetUserId} role changed from {oldRole} to {newRole}.");
    }

    public ProjectInvitation CreateInvitation(Guid invitedUserId, ProjectMemberRole roleOffered, Guid inviterUserId, string? message = null, TimeSpan? validity = null)
    {
        EnsureLeadOrOwner(inviterUserId);

        var existingMember = Members.FirstOrDefault(m => m.UserId == invitedUserId && m.Status == ProjectMembershipStatus.Active);
        if (existingMember != null)
            throw new DomainException("User is already an active member of this project.");

        var pendingInv = Invitations.FirstOrDefault(i => i.InvitedUserId == invitedUserId && i.Status == ProjectInvitationStatus.Pending && i.ExpiresAtUtc > DateTime.UtcNow);
        if (pendingInv != null)
            throw new DomainException("A pending invitation has already been sent to this researcher.");

        var invitation = new ProjectInvitation(Id, invitedUserId, inviterUserId, roleOffered, message, validity);
        Invitations.Add(invitation);

        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();
        LogActivity(inviterUserId, "InvitationCreated", $"Invited researcher {invitedUserId} with offered role {roleOffered}.");
        return invitation;
    }

    public ResearchMember AcceptInvitation(Guid invitationId, Guid invitedUserId)
    {
        var invitation = Invitations.FirstOrDefault(i => i.Id == invitationId && i.InvitedUserId == invitedUserId)
            ?? throw new DomainException("Invitation not found for this user.");

        if (invitation.Status != ProjectInvitationStatus.Pending)
            throw new DomainException($"Invitation cannot be accepted; current status is {invitation.Status}.");

        if (invitation.ExpiresAtUtc < DateTime.UtcNow)
        {
            invitation.Status = ProjectInvitationStatus.Expired;
            throw new DomainException("Invitation has expired.");
        }

        invitation.Status = ProjectInvitationStatus.Accepted;
        invitation.RespondedAtUtc = DateTime.UtcNow;

        var member = AddMemberInternal(invitedUserId, invitation.RoleOffered, invitation.InvitedByUserId);
        member.InvitedAtUtc = invitation.CreatedAtUtc;
        member.JoinedAtUtc = DateTime.UtcNow;

        LogActivity(invitedUserId, "InvitationAccepted", $"Researcher {invitedUserId} accepted project invitation as {invitation.RoleOffered}.");
        return member;
    }

    public void DeclineInvitation(Guid invitationId, Guid invitedUserId)
    {
        var invitation = Invitations.FirstOrDefault(i => i.Id == invitationId && i.InvitedUserId == invitedUserId)
            ?? throw new DomainException("Invitation not found for this user.");

        if (invitation.Status != ProjectInvitationStatus.Pending)
            throw new DomainException($"Invitation cannot be declined; current status is {invitation.Status}.");

        invitation.Status = ProjectInvitationStatus.Declined;
        invitation.RespondedAtUtc = DateTime.UtcNow;

        LogActivity(invitedUserId, "InvitationDeclined", $"Researcher {invitedUserId} declined project invitation.");
    }

    public ResearchTask AddTask(string title, Guid creatorUserId, string? description = null, Guid? assigneeUserId = null, TaskPriority priority = TaskPriority.Medium, DateTime? dueDateUtc = null, Guid? milestoneId = null)
    {
        EnsureMember(creatorUserId);

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Task title is required.");

        if (assigneeUserId.HasValue)
        {
            var isAssigneeMember = Members.Any(m => m.UserId == assigneeUserId.Value && m.Status == ProjectMembershipStatus.Active);
            if (!isAssigneeMember)
                throw new DomainException("Task assignee must be an active project member.");
        }

        if (milestoneId.HasValue)
        {
            var milestoneExists = Milestones.Any(m => m.Id == milestoneId.Value);
            if (!milestoneExists)
                throw new DomainException("Specified milestone does not exist on this project.");
        }

        var task = new ResearchTask(Id, title, creatorUserId, description, assigneeUserId, priority, dueDateUtc, milestoneId);
        Tasks.Add(task);

        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();
        LogActivity(creatorUserId, "TaskCreated", $"Task '{task.Title}' created.");
        return task;
    }

    public ResearchMilestone AddMilestone(string title, DateTime targetDateUtc, Guid creatorUserId, string? description = null)
    {
        EnsureLeadOrOwner(creatorUserId);

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Milestone title is required.");

        var milestone = new ResearchMilestone(Id, title, description, targetDateUtc, creatorUserId);
        Milestones.Add(milestone);

        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();
        LogActivity(creatorUserId, "MilestoneCreated", $"Milestone '{milestone.Title}' scheduled for {targetDateUtc:yyyy-MM-dd}.");
        return milestone;
    }

    public void LogActivity(Guid actorUserId, string action, string details)
    {
        var log = new ProjectActivityLog(Id, actorUserId, action, details);
        ActivityLogs.Add(log);
    }

    public void ValidateOwnership()
    {
        bool hasUserOwner = OwnerUserId.HasValue && OwnerUserId.Value != Guid.Empty;
        bool hasOrgOwner = OwnerOrganizationId.HasValue && OwnerOrganizationId.Value != Guid.Empty;

        if (hasUserOwner == hasOrgOwner)
        {
            throw new DomainException("A research project must have exactly one owner: either an individual user or an organization.");
        }
    }

    public bool IsLeadOrOwner(Guid userId)
    {
        if (OwnerUserId == userId)
            return true;

        return Members.Any(m => m.UserId == userId && m.Status == ProjectMembershipStatus.Active &&
            (m.Role == ProjectMemberRole.ProjectLead || m.Role == ProjectMemberRole.InstitutionalCoordinator));
    }

    public bool IsActiveMember(Guid userId)
    {
        if (OwnerUserId == userId)
            return true;

        return Members.Any(m => m.UserId == userId && m.Status == ProjectMembershipStatus.Active);
    }

    private void EnsureLeadOrOwner(Guid actorUserId)
    {
        if (!IsLeadOrOwner(actorUserId))
            throw new DomainException("Only the project lead or owner is authorized to perform this operation.");
    }

    private void EnsureMember(Guid actorUserId)
    {
        if (!IsActiveMember(actorUserId))
            throw new DomainException("Only active project members are authorized to perform this operation.");
    }
}
