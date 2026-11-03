using FluentAssertions;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Proposals;
using Pavena.Domain.Enums;
using Xunit;

namespace Pavena.UnitTests.Domain;

public class ResearchProposalTests
{
    private readonly Guid _challengeId = Guid.NewGuid();
    private readonly Guid _leadUserId = Guid.NewGuid();
    private readonly Guid _sponsorId = Guid.NewGuid();

    private ResearchProposal CreateSampleProposal()
    {
        return ResearchProposal.CreateDraft(
            challengeId: _challengeId,
            leadResearcherId: _leadUserId,
            submittingOrganizationId: null,
            title: "Advanced Biocatalytic Upcycling of Polyethylene Waste",
            slug: "advanced-biocatalytic-upcycling",
            abstractText: "This research proposes engineered bacterial consortia for low-temperature plastic enzymatic depolymerization.",
            problemStatement: "Current mechanical recycling results in significant polymer downcycling and toxic emissions.",
            researchObjectives: "1. Isolate novel esterases. 2. Establish 85% conversion yield at 37C within 72 hours.",
            proposedMethodology: "High-throughput metagenomic screening combined with directed protein evolution and HPLC validation.",
            expectedOutcomes: "Validated enzymatic formulation and open-access reaction kinetic dataset.",
            proposedTimelineMonths: 18,
            proposedBudget: 150000m,
            currency: "USD",
            fundingJustification: "Reagents ($40k), lab technician stipends ($70k), analytical spectroscopy fees ($40k)."
        );
    }

    [Fact]
    public void CreateDraft_InitializesWithCorrectDefaultValues()
    {
        var proposal = CreateSampleProposal();

        proposal.Status.Should().Be(ProposalStatus.Draft);
        proposal.SubmissionVersion.Should().Be(1);
        proposal.ChallengeId.Should().Be(_challengeId);
        proposal.LeadResearcherId.Should().Be(_leadUserId);
        proposal.Title.Should().Be("Advanced Biocatalytic Upcycling of Polyethylene Waste");
        proposal.ProposedBudget.Should().Be(150000m);
        proposal.ProposedTimelineMonths.Should().Be(18);
        proposal.Currency.Should().Be("USD");
        proposal.ConvertedProjectId.Should().BeNull();

        // Lead researcher automatically added as lead investigator
        proposal.Investigators.Should().HaveCount(1);
        var lead = proposal.Investigators.First();
        lead.UserId.Should().Be(_leadUserId);
        lead.Role.Should().Be(InvestigatorRole.LeadInvestigator);

        // Audit log created
        proposal.ActivityLogs.Should().HaveCount(1);
        proposal.ActivityLogs.First().ActivityType.Should().Be(ProposalActivityType.Created);
    }

    [Fact]
    public void UpdateDraft_WhenInDraft_UpdatesFieldsSuccessfully()
    {
        var proposal = CreateSampleProposal();

        proposal.UpdateDraft(
            _leadUserId,
            "Updated Biocatalytic Proposal Title",
            "updated-biocatalytic-proposal-title",
            "Updated abstract...",
            "Updated problem...",
            "Updated objectives...",
            "Updated methodology...",
            "Updated outcomes...",
            24,
            200000m,
            "EUR",
            "Updated funding justification...",
            ProposalConfidentialityLevel.RestrictedToSponsorAndReviewers,
            null);

        proposal.Title.Should().Be("Updated Biocatalytic Proposal Title");
        proposal.ProposedTimelineMonths.Should().Be(24);
        proposal.ProposedBudget.Should().Be(200000m);
        proposal.Currency.Should().Be("EUR");
        proposal.Confidentiality.Should().Be(ProposalConfidentialityLevel.RestrictedToSponsorAndReviewers);
        proposal.ActivityLogs.Should().Contain(l => l.ActivityType == ProposalActivityType.Updated);
    }

    [Fact]
    public void UpdateDraft_ByNonLeadResearcher_ThrowsDomainException()
    {
        var proposal = CreateSampleProposal();
        var anotherUser = Guid.NewGuid();

        var act = () => proposal.UpdateDraft(
            anotherUser,
            "Unauthorized Edit",
            "unauthorized-edit",
            "Abst...",
            "Prob...",
            "Obj...",
            "Meth...",
            "Out...",
            12,
            50000m,
            "USD",
            null,
            ProposalConfidentialityLevel.Confidential,
            null);

        act.Should().Throw<DomainException>()
            .WithMessage("*Only the Lead Researcher is authorized*");
    }

    [Fact]
    public void Submit_TransitionsToSubmitted_AndRecordsTimestamp()
    {
        var proposal = CreateSampleProposal();

        proposal.Submit(_leadUserId);

        proposal.Status.Should().Be(ProposalStatus.Submitted);
        proposal.SubmittedAtUtc.Should().NotBeNull();
        proposal.ActivityLogs.Should().Contain(l => l.ActivityType == ProposalActivityType.Submitted);
    }

    [Fact]
    public void PassEligibility_TransitionsToUnderReview()
    {
        var proposal = CreateSampleProposal();
        proposal.Submit(_leadUserId);

        proposal.PassEligibility(_sponsorId, "Verified institutional credentials.");

        proposal.Status.Should().Be(ProposalStatus.UnderReview);
        proposal.EligibilityReviewedAtUtc.Should().NotBeNull();
        proposal.EligibilityReviewedByUserId.Should().Be(_sponsorId);
        proposal.ActivityLogs.Should().Contain(l => l.ActivityType == ProposalActivityType.EligibilityPassed);
    }

    [Fact]
    public void FailEligibility_TransitionsToRejected_WithReason()
    {
        var proposal = CreateSampleProposal();
        proposal.Submit(_leadUserId);

        proposal.FailEligibility(_sponsorId, "University accreditation expired.");

        proposal.Status.Should().Be(ProposalStatus.Rejected);
        proposal.EligibilityRejectionReason.Should().Be("University accreditation expired.");
        proposal.ActivityLogs.Should().Contain(l => l.ActivityType == ProposalActivityType.EligibilityFailed);
    }

    [Fact]
    public void RequestRevision_AndResubmit_IncrementsVersion()
    {
        var proposal = CreateSampleProposal();
        proposal.Submit(_leadUserId);
        proposal.PassEligibility(_sponsorId);

        // Sponsor requests revision
        proposal.RequestRevision(_sponsorId, "Clarify bio-reactor scaling methodology and temperature control.");
        proposal.Status.Should().Be(ProposalStatus.RevisionRequested);
        proposal.RevisionGuidance.Should().Contain("Clarify bio-reactor scaling");

        // Lead researcher resubmits
        proposal.Resubmit(_leadUserId, "Added detailed 50L bioreactor P&ID and cooling jacket specifications.");
        proposal.Status.Should().Be(ProposalStatus.Resubmitted);
        proposal.SubmissionVersion.Should().Be(2);
        proposal.ResubmittedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Accept_AndReject_TransitionsStatusCorrectly()
    {
        var proposal = CreateSampleProposal();
        proposal.Submit(_leadUserId);
        proposal.PassEligibility(_sponsorId);

        proposal.Accept(_sponsorId, "Outstanding technical merit and clear path to pilot deployment.");
        proposal.Status.Should().Be(ProposalStatus.Accepted);
        proposal.DecisionNotes.Should().Contain("Outstanding technical merit");
        proposal.DecisionByUserId.Should().Be(_sponsorId);
        proposal.DecisionAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void MarkConverted_Succeeds_WhenProposalAccepted()
    {
        var proposal = CreateSampleProposal();
        proposal.Submit(_leadUserId);
        proposal.PassEligibility(_sponsorId);
        proposal.Accept(_sponsorId);

        var projectId = Guid.NewGuid();
        proposal.MarkConverted(projectId, _sponsorId);

        proposal.Status.Should().Be(ProposalStatus.ConvertedToProject);
        proposal.ConvertedProjectId.Should().Be(projectId);
        proposal.ActivityLogs.Should().Contain(l => l.ActivityType == ProposalActivityType.ConvertedToProject);
    }

    [Fact]
    public void Withdraw_TransitionsToWithdrawn()
    {
        var proposal = CreateSampleProposal();
        proposal.Submit(_leadUserId);

        proposal.Withdraw(_leadUserId, "PI relocation to different laboratory.");

        proposal.Status.Should().Be(ProposalStatus.Withdrawn);
        proposal.ActivityLogs.Should().Contain(l => l.ActivityType == ProposalActivityType.Withdrawn);
    }

    [Fact]
    public void AddInvestigator_PreventsDuplicateResearcher()
    {
        var proposal = CreateSampleProposal();
        var coInvestigatorId = Guid.NewGuid();

        proposal.AddInvestigator(coInvestigatorId, InvestigatorRole.CoInvestigator, "Enzyme purification specialist");
        proposal.Investigators.Should().HaveCount(2);

        var act = () => proposal.AddInvestigator(coInvestigatorId, InvestigatorRole.CoInvestigator, "Duplicate attempt");
        act.Should().Throw<DomainException>()
            .WithMessage("*already an investigator*");
    }

    [Fact]
    public void RemoveInvestigator_CannotRemoveLeadInvestigator()
    {
        var proposal = CreateSampleProposal();

        var act = () => proposal.RemoveInvestigator(_leadUserId);
        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot remove the Lead Investigator*");
    }
}
