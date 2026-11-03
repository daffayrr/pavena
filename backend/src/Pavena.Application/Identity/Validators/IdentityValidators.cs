using FluentValidation;
using Pavena.Application.Identity.DTOs;
using Pavena.Domain.Enums;

namespace Pavena.Application.Identity.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .Must(email => !email.Contains(' ')).WithMessage("Email cannot contain spaces.")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
            .MaximumLength(30).WithMessage("Username cannot exceed 30 characters.")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Username can only contain alphanumeric characters and underscores.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MinimumLength(2).WithMessage("Display name must be at least 2 characters.")
            .MaximumLength(150).WithMessage("Display name cannot exceed 150 characters.");

        RuleFor(x => x.CountryCode)
            .Length(2).When(x => !string.IsNullOrEmpty(x.CountryCode))
            .WithMessage("Country code must be a 2-letter ISO code.");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.EmailOrUsername)
            .NotEmpty().WithMessage("Email or username is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Reset token is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MinimumLength(2).WithMessage("Display name must be at least 2 characters.")
            .MaximumLength(150).WithMessage("Display name cannot exceed 150 characters.");

        RuleFor(x => x.CountryCode)
            .Length(2).When(x => !string.IsNullOrEmpty(x.CountryCode))
            .WithMessage("Country code must be a 2-letter ISO code.");

        RuleFor(x => x.Visibility)
            .IsEnumName(typeof(ProfileVisibility), caseSensitive: false)
            .WithMessage("Invalid profile visibility.");
    }
}

public class CreateEducationRequestValidator : AbstractValidator<CreateEducationRequest>
{
    public CreateEducationRequestValidator()
    {
        RuleFor(x => x.InstitutionName)
            .NotEmpty().WithMessage("Nama institusi pendidikan wajib diisi.")
            .MaximumLength(200).WithMessage("Nama institusi maksimal 200 karakter.");

        RuleFor(x => x.Degree)
            .MaximumLength(100).WithMessage("Gelar akademik maksimal 100 karakter.");

        RuleFor(x => x.FieldOfStudy)
            .MaximumLength(150).WithMessage("Bidang studi maksimal 150 karakter.");

        RuleFor(x => x.EndYear)
            .GreaterThanOrEqualTo(x => x.StartYear ?? 1900)
            .When(x => x.StartYear.HasValue && x.EndYear.HasValue)
            .WithMessage("Tahun lulus tidak boleh lebih awal dari tahun mulai.");
    }
}

public class CreateExperienceRequestValidator : AbstractValidator<CreateExperienceRequest>
{
    public CreateExperienceRequestValidator()
    {
        RuleFor(x => x.OrganizationName)
            .NotEmpty().WithMessage("Nama organisasi/lembaga riset wajib diisi.")
            .MaximumLength(200).WithMessage("Nama organisasi maksimal 200 karakter.");

        RuleFor(x => x.RoleTitle)
            .NotEmpty().WithMessage("Posisi/jabatan riset wajib diisi.")
            .MaximumLength(150).WithMessage("Posisi/jabatan maksimal 150 karakter.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Deskripsi pengalaman maksimal 2000 karakter.");
    }
}

public class CreateSkillRequestValidator : AbstractValidator<CreateSkillRequest>
{
    public CreateSkillRequestValidator()
    {
        RuleFor(x => x.SkillName)
            .NotEmpty().WithMessage("Nama keahlian riset wajib diisi.")
            .MinimumLength(2).WithMessage("Keahlian riset minimal 2 karakter.")
            .MaximumLength(100).WithMessage("Keahlian riset maksimal 100 karakter.");
    }
}
