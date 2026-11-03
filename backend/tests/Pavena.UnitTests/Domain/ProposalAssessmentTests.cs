using FluentAssertions;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Proposals;
using Pavena.Domain.Enums;
using Xunit;

namespace Pavena.UnitTests.Domain;

public class ProposalAssessmentTests
{
    private readonly Guid _proposalId = Guid.NewGuid();
    private readonly Guid _reviewerId = Guid.NewGuid();
    private readonly Guid _assignmentId = Guid.NewGuid();

    [Fact]
    public void CalculateOverallScore_ComputesMeanAcrossEightCriteriaScaledTo100()
    {
        var assessment = new ProposalAssessment
        {
            AssignmentId = _assignmentId,
            ProposalId = _proposalId,
            ReviewerUserId = _reviewerId,
            TechnicalMeritScore = 8.0m,
            MethodologyScore = 8.0m,
            FeasibilityScore = 8.0m,
            InnovationScore = 8.0m,
            BudgetAdequacyScore = 8.0m,
            TeamQualificationScore = 8.0m,
            ImpactPotentialScore = 8.0m,
            AlignmentWithChallengeScore = 8.0m
        };

        assessment.CalculateOverallScore();

        // 8 criteria * 8.0 = 64. (64 / 8) * 10 = 80.0
        assessment.OverallScore.Should().Be(80.0m);
    }

    [Fact]
    public void SubmitAssessment_WhenValid_SetsStatusSubmittedAndUpdatesAssignment()
    {
        var assignment = new ProposalReviewAssignment
        {
            Id = _assignmentId,
            ProposalId = _proposalId,
            ReviewerUserId = _reviewerId,
            Status = ReviewAssignmentStatus.Accepted
        };

        var assessment = new ProposalAssessment
        {
            AssignmentId = assignment.Id,
            ProposalId = _proposalId,
            ReviewerUserId = _reviewerId,
            Assignment = assignment,
            TechnicalMeritScore = 9.0m,
            MethodologyScore = 8.5m,
            FeasibilityScore = 8.0m,
            InnovationScore = 9.5m,
            BudgetAdequacyScore = 8.0m,
            TeamQualificationScore = 9.0m,
            ImpactPotentialScore = 8.5m,
            AlignmentWithChallengeScore = 9.0m,
            Strengths = "Novel metagenomic screening library and experienced PI.",
            Weaknesses = "Bioreactor scale-up validation requires external industrial partner.",
            RecommendationsForImprovement = "Include pilot benchmarking in early stage.",
            Recommendation = AssessmentRecommendation.RecommendAccept
        };

        assessment.SubmitAssessment();

        assessment.Status.Should().Be(AssessmentStatus.Submitted);
        assessment.SubmittedAtUtc.Should().NotBeNull();
        assignment.Status.Should().Be(ReviewAssignmentStatus.Completed);
    }

    [Fact]
    public void SubmitAssessment_WhenScoreOutOfRange_ThrowsDomainException()
    {
        var assessment = new ProposalAssessment
        {
            AssignmentId = _assignmentId,
            ProposalId = _proposalId,
            ReviewerUserId = _reviewerId,
            TechnicalMeritScore = 11.5m, // Out of range (> 10.0)
            MethodologyScore = 8.0m,
            FeasibilityScore = 8.0m,
            InnovationScore = 8.0m,
            BudgetAdequacyScore = 8.0m,
            TeamQualificationScore = 8.0m,
            ImpactPotentialScore = 8.0m,
            AlignmentWithChallengeScore = 8.0m,
            Strengths = "Great approach.",
            Weaknesses = "None noted.",
            RecommendationsForImprovement = "Continue work."
        };

        var act = () => assessment.SubmitAssessment();
        act.Should().Throw<DomainException>()
            .WithMessage("*must be between 0.0 and 10.0*");
    }

    [Fact]
    public void DeclareCoi_Recusal_SetsStatusRecused()
    {
        var assignment = new ProposalReviewAssignment
        {
            Id = _assignmentId,
            ProposalId = _proposalId,
            ReviewerUserId = _reviewerId,
            Status = ReviewAssignmentStatus.Assigned
        };

        assignment.RecuseDueToCoi(CoiDeclarationType.PastCollaboration, "Joint co-author on paper published last year.");

        assignment.Status.Should().Be(ReviewAssignmentStatus.Recused);
        assignment.HasCoiDeclaration.Should().BeTrue();
        assignment.CoiType.Should().Be(CoiDeclarationType.PastCollaboration);
        assignment.CoiDescription.Should().Contain("Joint co-author");
    }
}
