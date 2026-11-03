using FluentValidation;
using Pavena.Application.Research.DTOs;
using Pavena.Domain.Enums;

namespace Pavena.Application.Research.Validators;

public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Project title is required.")
            .MaximumLength(250).WithMessage("Project title cannot exceed 250 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Project slug is required.")
            .MaximumLength(120).WithMessage("Project slug cannot exceed 120 characters.")
            .Matches("^[a-z0-9-]+$").WithMessage("Slug can only contain lowercase alphanumeric characters and hyphens.");

        RuleFor(x => x.Visibility)
            .NotEmpty().WithMessage("Visibility is required.")
            .IsEnumName(typeof(ProjectVisibility), caseSensitive: false).WithMessage("Invalid project visibility.");

        When(x => !string.IsNullOrEmpty(x.ResearchField), () =>
        {
            RuleFor(x => x.ResearchField).MaximumLength(100).WithMessage("Research field cannot exceed 100 characters.");
        });
    }
}

public class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Project title is required.")
            .MaximumLength(250).WithMessage("Project title cannot exceed 250 characters.");

        RuleFor(x => x.Visibility)
            .NotEmpty().WithMessage("Visibility is required.")
            .IsEnumName(typeof(ProjectVisibility), caseSensitive: false).WithMessage("Invalid project visibility.");
    }
}

public class InviteMemberRequestValidator : AbstractValidator<InviteMemberRequest>
{
    public InviteMemberRequestValidator()
    {
        RuleFor(x => x.InvitedUserId)
            .NotEmpty().WithMessage("Invited user ID is required.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Offered role is required.")
            .IsEnumName(typeof(ProjectMemberRole), caseSensitive: false).WithMessage("Invalid project member role.");

        When(x => !string.IsNullOrEmpty(x.Message), () =>
        {
            RuleFor(x => x.Message).MaximumLength(500).WithMessage("Invitation message cannot exceed 500 characters.");
        });
    }
}

public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Task title is required.")
            .MaximumLength(250).WithMessage("Task title cannot exceed 250 characters.");

        RuleFor(x => x.Priority)
            .NotEmpty().WithMessage("Task priority is required.")
            .IsEnumName(typeof(TaskPriority), caseSensitive: false).WithMessage("Invalid task priority.");
    }
}

public class CreateMilestoneRequestValidator : AbstractValidator<CreateMilestoneRequest>
{
    public CreateMilestoneRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Milestone title is required.")
            .MaximumLength(250).WithMessage("Milestone title cannot exceed 250 characters.");

        RuleFor(x => x.TargetDateUtc)
            .NotEmpty().WithMessage("Target date is required.");
    }
}
