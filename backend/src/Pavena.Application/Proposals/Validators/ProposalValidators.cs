using FluentValidation;
using Pavena.Application.Proposals.DTOs;
using Pavena.Domain.Enums;

namespace Pavena.Application.Proposals.Validators;

public class CreateProposalDraftValidator : AbstractValidator<CreateProposalDraftRequest>
{
    public CreateProposalDraftValidator()
    {
        RuleFor(x => x.ChallengeId)
            .NotEmpty().WithMessage("Challenge ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Proposal title is required.")
            .MinimumLength(5).WithMessage("Title must be at least 5 characters.")
            .MaximumLength(300).WithMessage("Title must not exceed 300 characters.");

        RuleFor(x => x.Abstract)
            .NotEmpty().WithMessage("Proposal abstract is required.")
            .MinimumLength(20).WithMessage("Abstract must be at least 20 characters.");

        RuleFor(x => x.ProblemStatement)
            .NotEmpty().WithMessage("Problem statement is required.")
            .MinimumLength(30).WithMessage("Problem statement must be at least 30 characters.");

        RuleFor(x => x.ResearchObjectives)
            .NotEmpty().WithMessage("Research objectives are required.")
            .MinimumLength(20).WithMessage("Research objectives must be at least 20 characters.");

        RuleFor(x => x.ProposedMethodology)
            .NotEmpty().WithMessage("Proposed methodology is required.")
            .MinimumLength(30).WithMessage("Proposed methodology must be at least 30 characters.");

        RuleFor(x => x.ExpectedOutcomes)
            .NotEmpty().WithMessage("Expected outcomes are required.")
            .MinimumLength(20).WithMessage("Expected outcomes must be at least 20 characters.");

        RuleFor(x => x.ProposedTimelineMonths)
            .GreaterThan(0).WithMessage("Proposed timeline must be at least 1 month.")
            .LessThanOrEqualTo(120).WithMessage("Proposed timeline cannot exceed 120 months.");

        RuleFor(x => x.ProposedBudget)
            .GreaterThanOrEqualTo(0).WithMessage("Proposed budget cannot be negative.");

        RuleFor(x => x.Currency)
            .MaximumLength(10).WithMessage("Currency code must not exceed 10 characters.");
    }
}

public class UpdateProposalDraftValidator : AbstractValidator<UpdateProposalDraftRequest>
{
    public UpdateProposalDraftValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Proposal title is required.")
            .MinimumLength(5).WithMessage("Title must be at least 5 characters.")
            .MaximumLength(300).WithMessage("Title must not exceed 300 characters.");

        RuleFor(x => x.Abstract)
            .NotEmpty().WithMessage("Proposal abstract is required.")
            .MinimumLength(20).WithMessage("Abstract must be at least 20 characters.");

        RuleFor(x => x.ProblemStatement)
            .NotEmpty().WithMessage("Problem statement is required.")
            .MinimumLength(30).WithMessage("Problem statement must be at least 30 characters.");

        RuleFor(x => x.ResearchObjectives)
            .NotEmpty().WithMessage("Research objectives are required.")
            .MinimumLength(20).WithMessage("Research objectives must be at least 20 characters.");

        RuleFor(x => x.ProposedMethodology)
            .NotEmpty().WithMessage("Proposed methodology is required.")
            .MinimumLength(30).WithMessage("Proposed methodology must be at least 30 characters.");

        RuleFor(x => x.ExpectedOutcomes)
            .NotEmpty().WithMessage("Expected outcomes are required.")
            .MinimumLength(20).WithMessage("Expected outcomes must be at least 20 characters.");

        RuleFor(x => x.ProposedTimelineMonths)
            .GreaterThan(0).WithMessage("Proposed timeline must be at least 1 month.")
            .LessThanOrEqualTo(120).WithMessage("Proposed timeline cannot exceed 120 months.");

        RuleFor(x => x.ProposedBudget)
            .GreaterThanOrEqualTo(0).WithMessage("Proposed budget cannot be negative.");

        RuleFor(x => x.Currency)
            .MaximumLength(10).WithMessage("Currency code must not exceed 10 characters.");
    }
}

public class AddInvestigatorValidator : AbstractValidator<AddInvestigatorRequest>
{
    public AddInvestigatorValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Investigator user ID is required.");

        RuleFor(x => x.ContributionDescription)
            .MaximumLength(500).WithMessage("Contribution description must not exceed 500 characters.");
    }
}

public class ResubmitProposalValidator : AbstractValidator<ResubmitProposalRequest>
{
    public ResubmitProposalValidator()
    {
        RuleFor(x => x.ChangeSummary)
            .NotEmpty().WithMessage("Summary of changes is required for resubmission.")
            .MinimumLength(10).WithMessage("Change summary must be at least 10 characters.")
            .MaximumLength(1000).WithMessage("Change summary must not exceed 1000 characters.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Proposal title is required.")
            .MinimumLength(5).WithMessage("Title must be at least 5 characters.")
            .MaximumLength(300).WithMessage("Title must not exceed 300 characters.");

        RuleFor(x => x.Abstract)
            .NotEmpty().WithMessage("Abstract is required.");

        RuleFor(x => x.ProposedTimelineMonths)
            .GreaterThan(0).WithMessage("Proposed timeline must be at least 1 month.");

        RuleFor(x => x.ProposedBudget)
            .GreaterThanOrEqualTo(0).WithMessage("Proposed budget cannot be negative.");
    }
}

public class SubmitAssessmentValidator : AbstractValidator<SubmitAssessmentRequest>
{
    public SubmitAssessmentValidator()
    {
        RuleFor(x => x.TechnicalMeritScore).InclusiveBetween(0.0m, 10.0m).WithMessage("Technical merit score must be between 0.0 and 10.0.");
        RuleFor(x => x.MethodologyScore).InclusiveBetween(0.0m, 10.0m).WithMessage("Methodology score must be between 0.0 and 10.0.");
        RuleFor(x => x.FeasibilityScore).InclusiveBetween(0.0m, 10.0m).WithMessage("Feasibility score must be between 0.0 and 10.0.");
        RuleFor(x => x.InnovationScore).InclusiveBetween(0.0m, 10.0m).WithMessage("Innovation score must be between 0.0 and 10.0.");
        RuleFor(x => x.BudgetAdequacyScore).InclusiveBetween(0.0m, 10.0m).WithMessage("Budget adequacy score must be between 0.0 and 10.0.");
        RuleFor(x => x.TeamQualificationScore).InclusiveBetween(0.0m, 10.0m).WithMessage("Team qualification score must be between 0.0 and 10.0.");
        RuleFor(x => x.ImpactPotentialScore).InclusiveBetween(0.0m, 10.0m).WithMessage("Impact potential score must be between 0.0 and 10.0.");
        RuleFor(x => x.AlignmentWithChallengeScore).InclusiveBetween(0.0m, 10.0m).WithMessage("Alignment score must be between 0.0 and 10.0.");

        RuleFor(x => x.Strengths)
            .NotEmpty().WithMessage("Strengths evaluation commentary is required.")
            .MinimumLength(10).WithMessage("Strengths commentary must be at least 10 characters.");

        RuleFor(x => x.Weaknesses)
            .NotEmpty().WithMessage("Weaknesses evaluation commentary is required.")
            .MinimumLength(10).WithMessage("Weaknesses commentary must be at least 10 characters.");

        RuleFor(x => x.RecommendationsForImprovement)
            .NotEmpty().WithMessage("Recommendations for improvement are required.")
            .MinimumLength(10).WithMessage("Recommendations must be at least 10 characters.");
    }
}

public class SponsorDecisionValidator : AbstractValidator<SponsorDecisionRequest>
{
    public SponsorDecisionValidator()
    {
        RuleFor(x => x.Decision)
            .NotEmpty().WithMessage("Decision action is required.")
            .Must(d => d.Equals("Accept", StringComparison.OrdinalIgnoreCase) ||
                       d.Equals("Reject", StringComparison.OrdinalIgnoreCase) ||
                       d.Equals("RequestRevision", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Decision must be 'Accept', 'Reject', or 'RequestRevision'.");

        RuleFor(x => x.NotesOrGuidance)
            .NotEmpty().When(x => x.Decision.Equals("RequestRevision", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Revision guidance is mandatory when requesting revisions.");
    }
}

public class DeclareCoiValidator : AbstractValidator<DeclareCoiRequest>
{
    public DeclareCoiValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().When(x => x.HasConflict)
            .WithMessage("Conflict of interest description is required when declaring a conflict.");
    }
}
