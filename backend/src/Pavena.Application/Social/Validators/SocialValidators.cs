using FluentValidation;
using Pavena.Application.Social.DTOs;

namespace Pavena.Application.Social.Validators;

public class SendConnectionRequestValidator : AbstractValidator<SendConnectionRequest>
{
    public SendConnectionRequestValidator()
    {
        RuleFor(x => x.AddresseeUserId)
            .NotEmpty().WithMessage("Addressee user ID is required.");
    }
}

public class BlockUserRequestValidator : AbstractValidator<BlockUserRequest>
{
    public BlockUserRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(255).WithMessage("Block reason cannot exceed 255 characters.")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}
