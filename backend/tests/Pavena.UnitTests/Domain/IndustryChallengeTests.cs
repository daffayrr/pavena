using FluentAssertions;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Challenges;
using Pavena.Domain.Enums;
using Xunit;

namespace Pavena.UnitTests.Domain;

public class IndustryChallengeTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _creatorId = Guid.NewGuid();
    private readonly Guid _adminId = Guid.NewGuid();

    private IndustryChallenge CreateSampleChallenge()
    {
        return IndustryChallenge.CreateDraft(
            organizationId: _orgId,
            createdByUserId: _creatorId,
            title: "Subterranean Drone SLAM",
            slug: "subterranean-drone-slam",
            summary: "Decentralized SLAM in GPS-denied environments.",
            problemStatement: "Catastrophic drift within 50 meters of tunnel entry.",
            researchScope: "Underground tunnels and mines.",
            expectedOutcomes: "Simulation benchmarks and ROS2 algorithms.",
            submissionDeadlineUtc: DateTime.UtcNow.AddDays(30),
            visibility: ChallengeVisibility.Public,
            eligibility: InstitutionalEligibility.OpenToAll,
            currency: "USD",
            minBudget: 50000,
            maxBudget: 100000,
            estimatedDurationMonths: 12,
            requiresManualNdaApproval: false
        );
    }

    [Fact]
    public void CreateDraft_InitializesWithDraftStatusAndDefaultProperties()
    {
        var challenge = CreateSampleChallenge();

        challenge.Status.Should().Be(ChallengeStatus.Draft);
        challenge.OrganizationId.Should().Be(_orgId);
        challenge.CreatedByUserId.Should().Be(_creatorId);
        challenge.Title.Should().Be("Subterranean Drone SLAM");
        challenge.Slug.Should().Be("subterranean-drone-slam");
        challenge.Visibility.Should().Be(ChallengeVisibility.Public);
        challenge.Eligibility.Should().Be(InstitutionalEligibility.OpenToAll);
        challenge.MinBudget.Should().Be(50000);
        challenge.MaxBudget.Should().Be(100000);
        challenge.RequiresManualNdaApproval.Should().BeFalse();
        challenge.NdaTermsVersion.Should().Be("v1.0");
        challenge.ActivityLogs.Should().ContainSingle(l => l.ActionType == "DraftCreated");
    }

    [Fact]
    public void CreateDraft_WhenDeadlineInPast_ThrowsDomainException()
    {
        var act = () => IndustryChallenge.CreateDraft(
            _orgId,
            _creatorId,
            "Past Deadline Challenge",
            "past-deadline",
            "Summary",
            "Problem",
            "Scope",
            "Outcomes",
            DateTime.UtcNow.AddMinutes(-5)
        );

        act.Should().Throw<DomainException>().WithMessage("*future*");
    }

    [Fact]
    public void CreateDraft_WhenMinBudgetExceedsMaxBudget_ThrowsDomainException()
    {
        var act = () => IndustryChallenge.CreateDraft(
            _orgId,
            _creatorId,
            "Invalid Budget Challenge",
            "invalid-budget",
            "Summary",
            "Problem",
            "Scope",
            "Outcomes",
            DateTime.UtcNow.AddDays(10),
            minBudget: 100000,
            maxBudget: 50000
        );

        act.Should().Throw<DomainException>().WithMessage("*cannot be less than*");
    }

    [Fact]
    public void Lifecycle_FullTransition_FromDraftToClosed_Succeeds()
    {
        var challenge = CreateSampleChallenge();

        // 1. Submit for approval
        challenge.SubmitForApproval(_creatorId, "Ready for corporate review.");
        challenge.Status.Should().Be(ChallengeStatus.SubmittedForApproval);
        challenge.ActivityLogs.Should().Contain(l => l.ActionType == "SubmittedForApproval");

        // 2. Approve
        challenge.Approve(_adminId, "Technical scope looks solid.");
        challenge.Status.Should().Be(ChallengeStatus.Approved);
        challenge.Approvals.Should().ContainSingle(a => a.Decision == ChallengeApprovalDecision.Approved);

        // 3. Publish
        challenge.Publish(_adminId);
        challenge.Status.Should().Be(ChallengeStatus.Published);
        challenge.PublishedAtUtc.Should().NotBeNull();

        // 4. Open for Proposals
        challenge.OpenForProposals(_adminId);
        challenge.Status.Should().Be(ChallengeStatus.OpenForProposals);

        // 5. Close
        challenge.Close(_adminId, "Deadline reached, entering evaluation.");
        challenge.Status.Should().Be(ChallengeStatus.Closed);
        challenge.ClosedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Publish_DirectlyFromDraft_ThrowsDomainException()
    {
        var challenge = CreateSampleChallenge();

        var act = () => challenge.Publish(_adminId);

        act.Should().Throw<DomainException>().WithMessage("*approved*");
    }

    [Fact]
    public void RequestChanges_TransitionsToChangesRequested_AndLogsFeedback()
    {
        var challenge = CreateSampleChallenge();
        challenge.SubmitForApproval(_creatorId);

        challenge.RequestChanges(_adminId, "Please provide more details on deliverables.");

        challenge.Status.Should().Be(ChallengeStatus.ChangesRequested);
        challenge.Approvals.Should().ContainSingle(a => a.Decision == ChallengeApprovalDecision.ChangesRequested);
    }

    [Fact]
    public void Cancel_WhenValidReason_TransitionsToCancelled()
    {
        var challenge = CreateSampleChallenge();
        challenge.SubmitForApproval(_creatorId);
        challenge.Approve(_adminId);
        challenge.Publish(_adminId);

        challenge.Cancel(_adminId, "Budget realignment required by board.");

        challenge.Status.Should().Be(ChallengeStatus.Cancelled);
        challenge.CancelledAtUtc.Should().NotBeNull();
        challenge.CancellationReason.Should().Be("Budget realignment required by board.");
    }

    [Fact]
    public void Attachments_AddAndRemove_WorksCorrectly()
    {
        var challenge = CreateSampleChallenge();

        var att1 = ChallengeAttachment.Create(
            challenge.Id, "brief.pdf", ".pdf", "application/pdf", 1024 * 100, "storage/brief.pdf", AttachmentConfidentialityLevel.PublicBriefing, _creatorId, "Public brief");
        var att2 = ChallengeAttachment.Create(
            challenge.Id, "telemetry.csv", ".csv", "text/csv", 1024 * 5000, "storage/telemetry.csv", AttachmentConfidentialityLevel.ConfidentialNdaRequired, _creatorId, "Proprietary telemetry");

        challenge.Attachments.Add(att1);
        challenge.Attachments.Add(att2);

        challenge.Attachments.Should().HaveCount(2);
        att1.ConfidentialityLevel.Should().Be(AttachmentConfidentialityLevel.PublicBriefing);
        att2.ConfidentialityLevel.Should().Be(AttachmentConfidentialityLevel.ConfidentialNdaRequired);

        challenge.Attachments.Remove(att1);
        challenge.Attachments.Should().ContainSingle(a => a.Id == att2.Id);
    }

    [Fact]
    public void DigitalNda_RecordAgreement_DefaultAutoApproved_WhenRequiresManualApprovalFalse()
    {
        var challenge = CreateSampleChallenge();
        var researcherId = Guid.NewGuid();

        var agreement = ChallengeNdaAgreement.Create(
            challengeId: challenge.Id,
            researcherUserId: researcherId,
            ndaVersion: "1.0",
            termsContentHash: "ABCDEF123456",
            ipAddress: "127.0.0.1",
            requiresManualApproval: false
        );

        agreement.ResearcherUserId.Should().Be(researcherId);
        agreement.NdaVersion.Should().Be("1.0");
        agreement.TermsContentHash.Should().Be("ABCDEF123456");
        agreement.IsApprovedBySponsor.Should().BeTrue();
        agreement.ApprovedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void DigitalNda_RecordAgreement_PendingReview_WhenRequiresManualApprovalTrue()
    {
        var challenge = CreateSampleChallenge();
        var researcherId = Guid.NewGuid();

        var agreement = ChallengeNdaAgreement.Create(
            challengeId: challenge.Id,
            researcherUserId: researcherId,
            ndaVersion: "1.0",
            termsContentHash: "HASH999",
            ipAddress: "127.0.0.1",
            requiresManualApproval: true
        );

        agreement.IsApprovedBySponsor.Should().BeFalse();
        agreement.ApprovedAtUtc.Should().BeNull();

        agreement.Approve(_adminId);
        agreement.IsApprovedBySponsor.Should().BeTrue();
        agreement.ApprovedByUserId.Should().Be(_adminId);
        agreement.ApprovedAtUtc.Should().NotBeNull();
    }
}
