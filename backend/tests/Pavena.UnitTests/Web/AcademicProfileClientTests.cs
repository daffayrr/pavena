using FluentAssertions;
using Pavena.Application.Identity.DTOs;
using Pavena.Web.Services.Auth;
using Pavena.Web.Services.Profile;
using Xunit;

namespace Pavena.UnitTests.Web;

public class AcademicProfileClientTests
{
    private readonly IScholarSessionService _sessionService;
    private readonly AcademicProfileClient _client;

    public AcademicProfileClientTests()
    {
        _sessionService = new ScholarSessionService();
        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5181/") };
        _client = new AcademicProfileClient(httpClient, _sessionService);
    }

    [Fact]
    public async Task GetMyAcademicProfileAsync_ReturnsScholarProfileWithFullSections()
    {
        var profile = await _client.GetMyAcademicProfileAsync();

        profile.Should().NotBeNull();
        profile!.DisplayName.Should().Be("Dr. Marie Curie");
        profile.CompletenessScore.Should().BeGreaterThan(80);
        profile.Educations.Should().NotBeEmpty();
        profile.Experiences.Should().NotBeEmpty();
        profile.Skills.Should().NotBeEmpty();
        profile.Affiliations.Should().NotBeEmpty();
        profile.IsVerified.Should().BeTrue();
        profile.CanEdit.Should().BeTrue();
    }

    [Fact]
    public async Task GetAcademicProfileAsync_Colleague_ReturnsColleagueDetails()
    {
        var profile = await _client.GetAcademicProfileAsync("alan_turing");

        profile.Should().NotBeNull();
        profile!.DisplayName.Should().Be("Dr. Alan Turing");
        profile.Username.Should().Be("alan_turing");
        profile.IsVerified.Should().BeTrue();
        profile.CanEdit.Should().BeFalse();
        profile.Educations.Should().NotBeEmpty();
        profile.Experiences.Should().NotBeEmpty();
        profile.Skills.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AddAndDeleteEducation_ModifiesProfileStateCorrectly()
    {
        var req = new CreateEducationRequest(
            "Oxford University",
            "Honorary Doctorate",
            "Nuclear Physics",
            1922,
            1922,
            "Honoris Causa"
        );

        var added = await _client.AddEducationAsync(req);
        added.Should().NotBeNull();
        added!.InstitutionName.Should().Be("Oxford University");

        var profileAfterAdd = await _client.GetMyAcademicProfileAsync();
        profileAfterAdd!.Educations.Should().Contain(e => e.InstitutionName == "Oxford University");

        var deleteOk = await _client.DeleteEducationAsync(added.Id);
        deleteOk.Should().BeTrue();

        var profileAfterDelete = await _client.GetMyAcademicProfileAsync();
        profileAfterDelete!.Educations.Should().NotContain(e => e.Id == added.Id);
    }

    [Fact]
    public async Task AddAndDeleteExperience_ModifiesProfileStateCorrectly()
    {
        var req = new CreateExperienceRequest(
            "Laboratoire Curie",
            "Head of Research Laboratory",
            "Full-time",
            new DateTime(1914, 1, 1),
            null,
            true,
            "Radioelement isolation"
        );

        var added = await _client.AddExperienceAsync(req);
        added.Should().NotBeNull();
        added!.RoleTitle.Should().Be("Head of Research Laboratory");

        var profileAfterAdd = await _client.GetMyAcademicProfileAsync();
        profileAfterAdd!.Experiences.Should().Contain(e => e.RoleTitle == "Head of Research Laboratory");

        var deleteOk = await _client.DeleteExperienceAsync(added.Id);
        deleteOk.Should().BeTrue();

        var profileAfterDelete = await _client.GetMyAcademicProfileAsync();
        profileAfterDelete!.Experiences.Should().NotContain(e => e.Id == added.Id);
    }

    [Fact]
    public async Task AddAndDeleteSkill_ManagesSkillSetProperly()
    {
        var skillName = "Gamma Spectroscopy";
        var added = await _client.AddSkillAsync(skillName);
        added.Should().NotBeNull();
        added!.SkillName.Should().Be(skillName);

        var profileAfterAdd = await _client.GetMyAcademicProfileAsync();
        profileAfterAdd!.Skills.Should().Contain(s => s.SkillName == skillName);

        var deleteOk = await _client.DeleteSkillAsync(added.Id);
        deleteOk.Should().BeTrue();

        var profileAfterDelete = await _client.GetMyAcademicProfileAsync();
        profileAfterDelete!.Skills.Should().NotContain(s => s.Id == added.Id);
    }

    [Fact]
    public async Task UpdateProfileAsync_UpdatesBasicInformation()
    {
        var req = new UpdateProfileRequest(
            "Marie Skłodowska-Curie",
            "Professor of Nuclear Sciences & Radium Studies",
            "Updated research biography with international publications.",
            "FR",
            "Paris",
            "fr",
            "Public"
        );

        var updated = await _client.UpdateProfileAsync(req);
        updated.Should().NotBeNull();
        updated!.DisplayName.Should().Be("Marie Skłodowska-Curie");
        updated.Headline.Should().Be("Professor of Nuclear Sciences & Radium Studies");

        var current = await _client.GetMyAcademicProfileAsync();
        current!.DisplayName.Should().Be("Marie Skłodowska-Curie");
    }

    [Fact]
    public async Task ConnectionAndFollow_OperationsSucceed()
    {
        var colleagueId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var connectOk = await _client.SendConnectionRequestAsync(colleagueId);
        connectOk.Should().BeTrue();

        var acceptOk = await _client.AcceptConnectionRequestAsync(Guid.NewGuid());
        acceptOk.Should().BeTrue();

        var followOk = await _client.FollowUserAsync(colleagueId);
        followOk.Should().BeTrue();

        var unfollowOk = await _client.UnfollowUserAsync(colleagueId);
        unfollowOk.Should().BeTrue();
    }
}
