using FluentAssertions;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Research;
using Pavena.Domain.Enums;
using Xunit;

namespace Pavena.UnitTests.Domain;

public class ResearchProjectTests
{
    [Fact]
    public void CreatePersonal_SetsOwnerUserAndInitialLeadMember()
    {
        var userId = Guid.NewGuid();

        var project = ResearchProject.CreatePersonal("AI in Healthcare", "ai-in-healthcare", userId, "AI Healthcare abstract", "Computer Science", "Develop diagnostics model");

        project.Title.Should().Be("AI in Healthcare");
        project.Slug.Should().Be("ai-in-healthcare");
        project.OwnerUserId.Should().Be(userId);
        project.OwnerOrganizationId.Should().BeNull();
        project.OriginType.Should().Be(ProjectOriginType.ResearcherInitiated);
        project.Status.Should().Be(ProjectStatus.Active);
        project.Stage.Should().Be(ResearchStage.Concept);
        project.ResearchObjectives.Should().Be("Develop diagnostics model");

        project.Members.Should().ContainSingle();
        var lead = project.Members.First();
        lead.UserId.Should().Be(userId);
        lead.Role.Should().Be(ProjectMemberRole.ProjectLead);
        lead.Status.Should().Be(ProjectMembershipStatus.Active);

        project.ActivityLogs.Should().ContainSingle();
        project.ActivityLogs.First().Action.Should().Be("ProjectCreated");
    }

    [Fact]
    public void CreateInstitutional_SetsOwnerOrgAndNullUser()
    {
        var orgId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();

        var project = ResearchProject.CreateInstitutional("Quantum Materials Lab", "quantum-materials-lab", orgId, creatorId);

        project.Title.Should().Be("Quantum Materials Lab");
        project.OwnerOrganizationId.Should().Be(orgId);
        project.OwnerUserId.Should().BeNull();
        project.OriginType.Should().Be(ProjectOriginType.InstitutionalInitiated);
        project.Members.Should().Contain(m => m.UserId == creatorId && m.Role == ProjectMemberRole.InstitutionalCoordinator);
    }

    [Fact]
    public void CreateFromProposal_PreservesChallengeAndProposalTraceability()
    {
        var leadId = Guid.NewGuid();
        var challengeId = Guid.NewGuid();
        var proposalId = Guid.NewGuid();

        var project = ResearchProject.CreateFromProposal(
            "Low-Power Edge Neuromorphic Architecture",
            "edge-neuromorphic-2026",
            leadId,
            challengeId,
            proposalId,
            "Edge computing optimization",
            "Hardware Engineering",
            "Produce tapeout design");

        project.OriginType.Should().Be(ProjectOriginType.IndustryChallenge);
        project.SourceChallengeId.Should().Be(challengeId);
        project.SourceProposalId.Should().Be(proposalId);
        project.Status.Should().Be(ProjectStatus.Planning);
        project.Stage.Should().Be(ResearchStage.MethodologyDesign);
        project.Members.Should().Contain(m => m.UserId == leadId && m.Role == ProjectMemberRole.ProjectLead);
    }

    [Fact]
    public void ValidateOwnership_WithBothOwners_ThrowsDomainException()
    {
        var project = new ResearchProject
        {
            Title = "Invalid Dual Owned Project",
            Slug = "invalid-project",
            OwnerUserId = Guid.NewGuid(),
            OwnerOrganizationId = Guid.NewGuid()
        };

        Action act = () => project.ValidateOwnership();

        act.Should().Throw<DomainException>()
            .WithMessage("*must have exactly one owner*");
    }

    [Fact]
    public void ValidateOwnership_WithNoOwners_ThrowsDomainException()
    {
        var project = new ResearchProject
        {
            Title = "Orphan Project",
            Slug = "orphan-project",
            OwnerUserId = null,
            OwnerOrganizationId = null
        };

        Action act = () => project.ValidateOwnership();

        act.Should().Throw<DomainException>()
            .WithMessage("*must have exactly one owner*");
    }

    [Fact]
    public void TransitionStatus_AllowsPlanningToActiveToCompleted_AndBlocksDirectReactivationOfArchived()
    {
        var ownerId = Guid.NewGuid();
        var project = ResearchProject.CreatePersonal("Robotics Swarm", "robotics-swarm", ownerId);

        project.TransitionStatus(ProjectStatus.Planning, ownerId, "Refining scope");
        project.Status.Should().Be(ProjectStatus.Planning);

        project.TransitionStatus(ProjectStatus.Active, ownerId, "Field trial started");
        project.Status.Should().Be(ProjectStatus.Active);

        project.TransitionStatus(ProjectStatus.Completed, ownerId, "Milestones met");
        project.Status.Should().Be(ProjectStatus.Completed);

        project.TransitionStatus(ProjectStatus.Archived, ownerId, "Project retired");
        project.Status.Should().Be(ProjectStatus.Archived);

        // Attempting to move from Archived to Active directly must throw
        Action act = () => project.TransitionStatus(ProjectStatus.Active, ownerId);
        act.Should().Throw<DomainException>().WithMessage("*Cannot reactivate an archived project*");
    }

    [Fact]
    public void AdvanceStage_AdvancesScientificMaturity()
    {
        var ownerId = Guid.NewGuid();
        var project = ResearchProject.CreatePersonal("Cancer Immunotherapy", "cancer-immuno", ownerId);

        project.Stage.Should().Be(ResearchStage.Concept);
        project.AdvanceStage(ResearchStage.MethodologyDesign, ownerId);
        project.Stage.Should().Be(ResearchStage.MethodologyDesign);

        project.AdvanceStage(ResearchStage.IndustryPitchReady, ownerId);
        project.Stage.Should().Be(ResearchStage.IndustryPitchReady);

        project.ActivityLogs.Should().Contain(l => l.Action == "StageAdvanced");
    }

    [Fact]
    public void TransferLeadership_UpdatesRolesAndOwnership()
    {
        var ownerA = Guid.NewGuid();
        var researcherB = Guid.NewGuid();
        var project = ResearchProject.CreatePersonal("Battery Cathode Study", "battery-study", ownerA);

        project.AddMember(researcherB, ProjectMemberRole.CoResearcher, ownerA);

        project.TransferLeadership(ownerA, researcherB);

        project.OwnerUserId.Should().Be(researcherB);
        project.Members.First(m => m.UserId == researcherB).Role.Should().Be(ProjectMemberRole.ProjectLead);
        project.Members.First(m => m.UserId == ownerA).Role.Should().Be(ProjectMemberRole.CoResearcher);
        project.ActivityLogs.Should().Contain(l => l.Action == "LeadershipTransferred");
    }

    [Fact]
    public void MemberInvitations_CreateAcceptDecline_LifecycleWorksCorrectly()
    {
        var owner = Guid.NewGuid();
        var colleague = Guid.NewGuid();
        var project = ResearchProject.CreatePersonal("Climate Modeling", "climate-model", owner);

        // 1. Create invitation
        var inv = project.CreateInvitation(colleague, ProjectMemberRole.Researcher, owner, "Join our team!");
        inv.Status.Should().Be(ProjectInvitationStatus.Pending);
        inv.RoleOffered.Should().Be(ProjectMemberRole.Researcher);

        // Cannot send duplicate pending invitation
        Action duplicateAct = () => project.CreateInvitation(colleague, ProjectMemberRole.Researcher, owner);
        duplicateAct.Should().Throw<DomainException>().WithMessage("*pending invitation has already been sent*");

        // 2. Accept invitation
        var member = project.AcceptInvitation(inv.Id, colleague);
        member.Should().NotBeNull();
        member.UserId.Should().Be(colleague);
        member.Role.Should().Be(ProjectMemberRole.Researcher);
        member.Status.Should().Be(ProjectMembershipStatus.Active);
        inv.Status.Should().Be(ProjectInvitationStatus.Accepted);
    }

    [Fact]
    public void RemoveMember_SetsRemovedStatus_AndPreventsFurtherActions()
    {
        var owner = Guid.NewGuid();
        var assistant = Guid.NewGuid();
        var project = ResearchProject.CreatePersonal("Microbiome Analysis", "microbiome-analysis", owner);

        project.AddMember(assistant, ProjectMemberRole.ResearchAssistant, owner);

        project.RemoveMember(assistant, owner);

        var memberRecord = project.Members.First(m => m.UserId == assistant);
        memberRecord.Status.Should().Be(ProjectMembershipStatus.Removed);
        memberRecord.LeftAtUtc.Should().NotBeNull();

        // Project lead cannot be removed without transfer
        Action removeLeadAct = () => project.RemoveMember(owner, owner);
        removeLeadAct.Should().Throw<DomainException>().WithMessage("*Cannot remove the primary project owner*");
    }

    [Fact]
    public void TaskAndMilestoneManagement_EnforcesInvariants()
    {
        var owner = Guid.NewGuid();
        var member = Guid.NewGuid();
        var stranger = Guid.NewGuid();
        var project = ResearchProject.CreatePersonal("Neural Rendering", "neural-rendering", owner);
        project.AddMember(member, ProjectMemberRole.Researcher, owner);

        // 1. Add Milestone
        var milestone = project.AddMilestone("Beta Model Release", DateTime.UtcNow.AddMonths(2), owner, "First release candidate");
        milestone.Status.Should().Be(MilestoneStatus.Planned);

        // 2. Add Task linked to milestone
        var task = project.AddTask("Train NeRF baseline", member, "Run cluster training", member, TaskPriority.High, DateTime.UtcNow.AddDays(7), milestone.Id);
        task.Title.Should().Be("Train NeRF baseline");
        task.Priority.Should().Be(TaskPriority.High);
        task.MilestoneId.Should().Be(milestone.Id);

        // 3. Assign task to stranger must fail
        Action assignStrangerAct = () => project.AddTask("Invalid Task", member, "Description", stranger);
        assignStrangerAct.Should().Throw<DomainException>().WithMessage("*must be an active project member*");

        // 4. Stranger cannot add tasks
        Action strangerAddTaskAct = () => project.AddTask("Hacker Task", stranger);
        strangerAddTaskAct.Should().Throw<DomainException>().WithMessage("*Only active project members*");
    }
}
