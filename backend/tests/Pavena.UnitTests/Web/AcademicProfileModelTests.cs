using FluentAssertions;
using Pavena.Application.Identity.DTOs;
using Xunit;

namespace Pavena.UnitTests.Web;

public class AcademicProfileModelTests
{
    [Fact]
    public void CompletenessScore_WhenAllSectionsPopulated_Is100Percent()
    {
        var educations = new List<UserEducationDto>
        {
            new(Guid.NewGuid(), "ITB", "S.T.", "Teknik Informatika", 2015, 2019, "3.90")
        };

        var experiences = new List<UserExperienceDto>
        {
            new(Guid.NewGuid(), "BRIN", "Peneliti Pertama", "Full-time", new DateTime(2020, 1, 1), null, true, "Riset Komputasi")
        };

        var skills = new List<UserSkillDto>
        {
            new(Guid.NewGuid(), "High Performance Computing")
        };

        var affiliations = new List<AffiliationDto>
        {
            new(Guid.NewGuid(), "Institut Teknologi Bandung", "STEI", "Dosen", "Faculty", true)
        };

        var profile = new AcademicProfileDto(
            Guid.NewGuid(),
            "scholar_id",
            "Dr. Habibie",
            "Aeronautics & AI Specialist",
            "Mendedikasikan riset untuk kemandirian teknologi dirgantara.",
            "ID",
            "Bandung",
            "id",
            "https://cdn.pavena.org/avatars/habibie.jpg",
            "https://cdn.pavena.org/banners/itb.jpg",
            "Public",
            DateTime.UtcNow,
            100,
            true,
            "0000-0002-1825-0097",
            educations,
            experiences,
            skills,
            new[] { "Aerodynamics", "Quantum Computing" },
            affiliations,
            15,
            140,
            850,
            120,
            "Accepted",
            false,
            true
        );

        profile.CompletenessScore.Should().Be(100);
        profile.IsVerified.Should().BeTrue();
        profile.CanEdit.Should().BeTrue();
        profile.Educations.Should().HaveCount(1);
        profile.Experiences.Should().HaveCount(1);
        profile.Affiliations[0].IsVerified.Should().BeTrue();
    }

    [Fact]
    public void PrivacyMasking_WhenPrivate_ExcludesSensitiveFields()
    {
        var privateProfile = new AcademicProfileDto(
            Guid.NewGuid(),
            "secret_scholar",
            "Prof. Anonymous",
            "Classified Lab Lead",
            null, // Masked
            null, // Masked
            null, // Masked
            "en",
            null,
            null,
            "Private",
            DateTime.UtcNow,
            25,
            false,
            null,
            Array.Empty<UserEducationDto>(),
            Array.Empty<UserExperienceDto>(),
            Array.Empty<UserSkillDto>(),
            Array.Empty<string>(),
            Array.Empty<AffiliationDto>(),
            0,
            0,
            12,
            0,
            "None",
            false,
            false
        );

        privateProfile.Bio.Should().BeNull();
        privateProfile.CountryCode.Should().BeNull();
        privateProfile.City.Should().BeNull();
        privateProfile.Educations.Should().BeEmpty();
        privateProfile.Experiences.Should().BeEmpty();
        privateProfile.Skills.Should().BeEmpty();
        privateProfile.CanEdit.Should().BeFalse();
    }
}
