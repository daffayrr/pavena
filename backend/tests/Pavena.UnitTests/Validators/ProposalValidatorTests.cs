using FluentAssertions;
using Pavena.Application.Proposals.DTOs;
using Pavena.Application.Proposals.Validators;
using Pavena.Domain.Enums;
using Xunit;

namespace Pavena.UnitTests.Validators;

public class ProposalValidatorTests
{
    private readonly CreateProposalDraftValidator _createValidator = new();
    private readonly SubmitAssessmentValidator _assessmentValidator = new();
    private readonly SponsorDecisionValidator _decisionValidator = new();

    [Fact]
    public void CreateProposalDraftValidator_ValidRequest_PassesValidation()
    {
        var request = new CreateProposalDraftRequest(
            ChallengeId: Guid.NewGuid(),
            Title: "Scalable Direct Air Carbon Capture Sorbents",
            Abstract: "A comprehensive investigation into metal-organic frameworks for atmospheric carbon capture.",
            ProblemStatement: "High energy regeneration penalty remains the key bottleneck in solid sorbent DAC.",
            ResearchObjectives: "Synthesize moisture-tolerant amine-functionalized MOFs with <60 kJ/mol desorption energy.",
            ProposedMethodology: "Computational DFT screening followed by solvothermal synthesis and fixed-bed breakthrough testing.",
            ExpectedOutcomes: "2 patented sorbent chemistries and fixed-bed cycling dataset under tropical humidity.",
            ProposedTimelineMonths: 24,
            ProposedBudget: 250000m,
            Currency: "USD"
        );

        var result = _createValidator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateProposalDraftValidator_EmptyTitleOrNegativeBudget_FailsValidation()
    {
        var request = new CreateProposalDraftRequest(
            ChallengeId: Guid.NewGuid(),
            Title: "",
            Abstract: "Valid abstract with enough characters.",
            ProblemStatement: "Valid problem statement with enough characters.",
            ResearchObjectives: "Valid research objectives with enough characters.",
            ProposedMethodology: "Valid proposed methodology with enough characters.",
            ExpectedOutcomes: "Valid expected outcomes with enough characters.",
            ProposedTimelineMonths: 12,
            ProposedBudget: -500m
        );

        var result = _createValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
        result.Errors.Should().Contain(e => e.PropertyName == "ProposedBudget");
    }

    [Fact]
    public void SubmitAssessmentValidator_ScoresWithinZeroToTen_PassesValidation()
    {
        var request = new SubmitAssessmentRequest(
            TechnicalMeritScore: 8.5m,
            MethodologyScore: 9.0m,
            FeasibilityScore: 7.5m,
            InnovationScore: 9.0m,
            BudgetAdequacyScore: 8.0m,
            TeamQualificationScore: 8.5m,
            ImpactPotentialScore: 8.0m,
            AlignmentWithChallengeScore: 9.0m,
            Strengths: "Exceptional PI publication record and rigorous chemical characterization protocols.",
            Weaknesses: "Thermal cycler longevity data not addressed in preliminary analysis.",
            RecommendationsForImprovement: "Provide accelerated hydrothermal stability testing in revised milestones.",
            ConfidentialRemarksForSponsor: "Recommend prioritizing this proposal for full pilot funding.",
            Recommendation: AssessmentRecommendation.RecommendAccept
        );

        var result = _assessmentValidator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SubmitAssessmentValidator_ScoreGreaterThanTen_FailsValidation()
    {
        var request = new SubmitAssessmentRequest(
            TechnicalMeritScore: 10.5m, // Invalid (> 10.0)
            MethodologyScore: 9.0m,
            FeasibilityScore: 7.5m,
            InnovationScore: 9.0m,
            BudgetAdequacyScore: 8.0m,
            TeamQualificationScore: 8.5m,
            ImpactPotentialScore: 8.0m,
            AlignmentWithChallengeScore: 9.0m,
            Strengths: "Exceptional PI publication record and rigorous protocols.",
            Weaknesses: "Thermal cycler longevity data not addressed in preliminary analysis.",
            RecommendationsForImprovement: "Provide accelerated hydrothermal stability testing.",
            ConfidentialRemarksForSponsor: null,
            Recommendation: AssessmentRecommendation.RecommendAccept
        );

        var result = _assessmentValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TechnicalMeritScore");
    }

    [Fact]
    public void SponsorDecisionValidator_InvalidAction_FailsValidation()
    {
        var request = new SponsorDecisionRequest("InvalidAction", "Some notes");
        var result = _decisionValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Decision");
    }

    [Fact]
    public void SponsorDecisionValidator_RequestRevisionWithoutGuidance_FailsValidation()
    {
        var request = new SponsorDecisionRequest("RequestRevision", "");
        var result = _decisionValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NotesOrGuidance");
    }
}
