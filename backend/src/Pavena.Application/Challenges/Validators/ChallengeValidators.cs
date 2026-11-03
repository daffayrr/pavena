using FluentValidation;
using Pavena.Application.Challenges.DTOs;

namespace Pavena.Application.Challenges.Validators;

public class CreateChallengeValidator : AbstractValidator<CreateChallengeRequest>
{
    public CreateChallengeValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("Organization ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Challenge title is required.")
            .MinimumLength(5).WithMessage("Title must be at least 5 characters.")
            .MaximumLength(300).WithMessage("Title must not exceed 300 characters.");

        RuleFor(x => x.Summary)
            .NotEmpty().WithMessage("Challenge summary is required.")
            .MinimumLength(20).WithMessage("Summary must be at least 20 characters.")
            .MaximumLength(1000).WithMessage("Summary must not exceed 1000 characters.");

        RuleFor(x => x.ProblemStatement)
            .NotEmpty().WithMessage("Problem statement is required.")
            .MinimumLength(50).WithMessage("Problem statement must be at least 50 characters.");

        RuleFor(x => x.ResearchScope)
            .NotEmpty().WithMessage("Research scope is required.")
            .MinimumLength(30).WithMessage("Research scope must be at least 30 characters.");

        RuleFor(x => x.ExpectedOutcomes)
            .NotEmpty().WithMessage("Expected outcomes are required.")
            .MinimumLength(20).WithMessage("Expected outcomes must be at least 20 characters.");

        RuleFor(x => x.SubmissionDeadlineUtc)
            .Must(d => d > DateTime.UtcNow).WithMessage("Submission deadline must be in the future.");

        RuleFor(x => x.MinBudget)
            .GreaterThanOrEqualTo(0).When(x => x.MinBudget.HasValue)
            .WithMessage("Minimum budget cannot be negative.");

        RuleFor(x => x.MaxBudget)
            .GreaterThanOrEqualTo(0).When(x => x.MaxBudget.HasValue)
            .WithMessage("Maximum budget cannot be negative.");

        RuleFor(x => x)
            .Must(x => !x.MinBudget.HasValue || !x.MaxBudget.HasValue || x.MaxBudget.Value >= x.MinBudget.Value)
            .WithMessage("Maximum budget cannot be less than minimum budget.");
    }
}

public class UpdateChallengeValidator : AbstractValidator<UpdateChallengeRequest>
{
    public UpdateChallengeValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Challenge title is required.")
            .MinimumLength(5).WithMessage("Title must be at least 5 characters.")
            .MaximumLength(300).WithMessage("Title must not exceed 300 characters.");

        RuleFor(x => x.Summary)
            .NotEmpty().WithMessage("Challenge summary is required.")
            .MinimumLength(20).WithMessage("Summary must be at least 20 characters.")
            .MaximumLength(1000).WithMessage("Summary must not exceed 1000 characters.");

        RuleFor(x => x.ProblemStatement)
            .NotEmpty().WithMessage("Problem statement is required.")
            .MinimumLength(50).WithMessage("Problem statement must be at least 50 characters.");

        RuleFor(x => x.ResearchScope)
            .NotEmpty().WithMessage("Research scope is required.")
            .MinimumLength(30).WithMessage("Research scope must be at least 30 characters.");

        RuleFor(x => x.ExpectedOutcomes)
            .NotEmpty().WithMessage("Expected outcomes are required.")
            .MinimumLength(20).WithMessage("Expected outcomes must be at least 20 characters.");

        RuleFor(x => x.SubmissionDeadlineUtc)
            .Must(d => d > DateTime.UtcNow).WithMessage("Submission deadline must be in the future.");

        RuleFor(x => x)
            .Must(x => !x.MinBudget.HasValue || !x.MaxBudget.HasValue || x.MaxBudget.Value >= x.MinBudget.Value)
            .WithMessage("Maximum budget cannot be less than minimum budget.");
    }
}

public class CancelChallengeValidator : AbstractValidator<CancelChallengeRequest>
{
    public CancelChallengeValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A cancellation reason is required.")
            .MinimumLength(10).WithMessage("Cancellation reason must be at least 10 characters.")
            .MaximumLength(1000).WithMessage("Cancellation reason must not exceed 1000 characters.");
    }
}

public class SignNdaValidator : AbstractValidator<SignNdaRequest>
{
    public SignNdaValidator()
    {
        RuleFor(x => x.NdaVersion)
            .NotEmpty().WithMessage("NDA version is required.");

        RuleFor(x => x.TermsContentHash)
            .NotEmpty().WithMessage("Terms content hash is required.");
    }
}
