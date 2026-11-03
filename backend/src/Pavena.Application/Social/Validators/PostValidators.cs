using FluentValidation;
using Pavena.Application.Social.DTOs;
using Pavena.Domain.Entities.Social;
using Pavena.Domain.Enums;

namespace Pavena.Application.Social.Validators;

public class CreatePostRequestValidator : AbstractValidator<CreatePostRequest>
{
    public CreatePostRequestValidator()
    {
        RuleFor(x => x.Content)
            .MaximumLength(Post.MaxContentLength).WithMessage($"Post content cannot exceed {Post.MaxContentLength} characters.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Published post content cannot be empty.")
            .When(x => x.PublishImmediately);

        RuleFor(x => x.Visibility)
            .NotEmpty().WithMessage("Post visibility is required.")
            .IsEnumName(typeof(PostVisibility), caseSensitive: false).WithMessage("Visibility must be 'Public', 'Connections', or 'OnlyMe'.");

        RuleFor(x => x.Attachments)
            .Must(list => list == null || list.Count <= Post.MaxAttachmentsCount)
            .WithMessage($"Cannot attach more than {Post.MaxAttachmentsCount} files.");

        RuleForEach(x => x.Attachments)
            .SetValidator(new CreateAttachmentRequestValidator())
            .When(x => x.Attachments != null);
    }
}

public class CreateAttachmentRequestValidator : AbstractValidator<CreateAttachmentRequest>
{
    public CreateAttachmentRequestValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.")
            .MaximumLength(255).WithMessage("File name cannot exceed 255 characters.");

        RuleFor(x => x.StorageKey)
            .NotEmpty().WithMessage("Storage key is required.")
            .MaximumLength(500).WithMessage("Storage key cannot exceed 500 characters.");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0).WithMessage("File size must be greater than 0.")
            .LessThanOrEqualTo(Post.MaxAttachmentSizeBytes).WithMessage($"File size cannot exceed {Post.MaxAttachmentSizeBytes / (1024 * 1024)} MB.");
    }
}

public class UpdatePostRequestValidator : AbstractValidator<UpdatePostRequest>
{
    public UpdatePostRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Post content cannot be empty.")
            .MaximumLength(Post.MaxContentLength).WithMessage($"Post content cannot exceed {Post.MaxContentLength} characters.");

        RuleFor(x => x.Visibility)
            .NotEmpty().WithMessage("Post visibility is required.")
            .IsEnumName(typeof(PostVisibility), caseSensitive: false).WithMessage("Visibility must be 'Public', 'Connections', or 'OnlyMe'.");
    }
}
