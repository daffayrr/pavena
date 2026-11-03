using FluentAssertions;
using Pavena.Application.Challenges.DTOs;
using Pavena.Application.Challenges.Validators;
using Pavena.Domain.Enums;
using Xunit;

namespace Pavena.UnitTests.Validators;

public class ChallengeValidatorTests
{
    private readonly CreateChallengeValidator _createValidator = new();
    private readonly UpdateChallengeValidator _updateValidator = new();
    private readonly CancelChallengeValidator _cancelValidator = new();
    private readonly SignNdaValidator _signNdaValidator = new();

    [Fact]
    public void CreateChallengeValidator_ValidRequest_Passes()
    {
        var req = new CreateChallengeRequest(
            OrganizationId: Guid.NewGuid(),
            Title: "Autonomous Underwater Navigation with Acoustic Modems",
            Summary: "A collaborative challenge seeking marine roboticists to improve subsea localization.",
            ProblemStatement: "Acoustic positioning degrades significantly in shallow water reverberation environments.",
            ResearchScope: "Shallow water littoral zones and river estuaries.",
            ExpectedOutcomes: "Validated sensor fusion algorithms and benchmark datasets.",
            SubmissionDeadlineUtc: DateTime.UtcNow.AddDays(45),
            MinBudget: 30000,
            MaxBudget: 80000
        );

        var result = _createValidator.Validate(req);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateChallengeValidator_WhenTitleTooShort_Fails()
    {
        var req = new CreateChallengeRequest(
            OrganizationId: Guid.NewGuid(),
            Title: "UAV",
            Summary: "A collaborative challenge seeking marine roboticists to improve subsea localization.",
            ProblemStatement: "Acoustic positioning degrades significantly in shallow water reverberation environments.",
            ResearchScope: "Shallow water littoral zones and river estuaries.",
            ExpectedOutcomes: "Validated sensor fusion algorithms and benchmark datasets.",
            SubmissionDeadlineUtc: DateTime.UtcNow.AddDays(45)
        );

        var result = _createValidator.Validate(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(req.Title));
    }

    [Fact]
    public void CreateChallengeValidator_WhenMinBudgetExceedsMaxBudget_Fails()
    {
        var req = new CreateChallengeRequest(
            OrganizationId: Guid.NewGuid(),
            Title: "Autonomous Underwater Navigation with Acoustic Modems",
            Summary: "A collaborative challenge seeking marine roboticists to improve subsea localization.",
            ProblemStatement: "Acoustic positioning degrades significantly in shallow water reverberation environments.",
            ResearchScope: "Shallow water littoral zones and river estuaries.",
            ExpectedOutcomes: "Validated sensor fusion algorithms and benchmark datasets.",
            SubmissionDeadlineUtc: DateTime.UtcNow.AddDays(45),
            MinBudget: 90000,
            MaxBudget: 50000
        );

        var result = _createValidator.Validate(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("cannot be less than"));
    }

    [Fact]
    public void CancelChallengeValidator_WhenReasonTooShort_Fails()
    {
        var req = new CancelChallengeRequest("Short");

        var result = _cancelValidator.Validate(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(req.Reason));
    }

    [Fact]
    public void SignNdaValidator_WhenMissingVersionOrHash_Fails()
    {
        var req = new SignNdaRequest("", "");

        var result = _signNdaValidator.Validate(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void SignNdaValidator_ValidRequest_Passes()
    {
        var req = new SignNdaRequest("1.0", "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855");

        var result = _signNdaValidator.Validate(req);

        result.IsValid.Should().BeTrue();
    }
}
