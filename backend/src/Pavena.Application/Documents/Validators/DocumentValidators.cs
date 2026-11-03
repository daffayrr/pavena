using FluentValidation;
using Pavena.Application.Documents.DTOs;

namespace Pavena.Application.Documents.Validators;

public class CreateDocumentRequestValidator : AbstractValidator<CreateDocumentRequest>
{
    public CreateDocumentRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Document title is required.")
            .MaximumLength(250).WithMessage("Document title cannot exceed 250 characters.");

        When(x => !string.IsNullOrEmpty(x.Description), () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");
        });

        When(x => !string.IsNullOrEmpty(x.DocumentType), () =>
        {
            RuleFor(x => x.DocumentType)
                .MaximumLength(50).WithMessage("Document type cannot exceed 50 characters.");
        });
    }
}

public class UpdateDocumentRequestValidator : AbstractValidator<UpdateDocumentRequest>
{
    public UpdateDocumentRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Document title is required.")
            .MaximumLength(250).WithMessage("Document title cannot exceed 250 characters.");

        When(x => !string.IsNullOrEmpty(x.Description), () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");
        });

        RuleFor(x => x.ConcurrencyStamp)
            .NotEmpty().WithMessage("Concurrency stamp is required.");
    }
}

public class CreateFileRequestValidator : AbstractValidator<CreateFileRequest>
{
    public CreateFileRequestValidator()
    {
        RuleFor(x => x.Path)
            .NotEmpty().WithMessage("File path is required.")
            .MaximumLength(500).WithMessage("File path cannot exceed 500 characters.")
            .Must(NotContainIllegalPathCharacters).WithMessage("File path contains illegal characters.")
            .Must(NotTraverseDirectories).WithMessage("Path traversal is not permitted.");
    }

    private static bool NotContainIllegalPathCharacters(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        var invalid = new[] { ':', '*', '?', '"', '<', '>', '|' };
        return !path.Any(c => invalid.Contains(c));
    }

    private static bool NotTraverseDirectories(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        var normalized = path.Replace('\\', '/');
        return !normalized.Contains("../") && !normalized.Contains("/..") && !normalized.StartsWith("../");
    }
}

public class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Comment content is required.")
            .MaximumLength(4000).WithMessage("Comment content cannot exceed 4000 characters.");

        When(x => !string.IsNullOrEmpty(x.SelectedText), () =>
        {
            RuleFor(x => x.SelectedText)
                .MaximumLength(1000).WithMessage("Selected text cannot exceed 1000 characters.");
        });
    }
}

public class CreateVersionRequestValidator : AbstractValidator<CreateVersionRequest>
{
    public CreateVersionRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Version title is required.")
            .MaximumLength(200).WithMessage("Version title cannot exceed 200 characters.");

        When(x => !string.IsNullOrEmpty(x.Description), () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");
        });
    }
}
