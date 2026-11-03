using FluentAssertions;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Research;
using Pavena.Domain.Enums;
using Xunit;

namespace Pavena.UnitTests.Domain;

public class ResearchProjectTests
{
    [Fact]
    public void CreatePersonal_SetsOwnerUserAndNullOrg()
    {
        var userId = Guid.NewGuid();

        var project = ResearchProject.CreatePersonal("AI in Healthcare", "ai-in-healthcare", userId);

        project.Title.Should().Be("AI in Healthcare");
        project.Slug.Should().Be("ai-in-healthcare");
        project.OwnerUserId.Should().Be(userId);
        project.OwnerOrganizationId.Should().BeNull();
        project.Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public void CreateInstitutional_SetsOwnerOrgAndNullUser()
    {
        var orgId = Guid.NewGuid();

        var project = ResearchProject.CreateInstitutional("Quantum Materials Lab", "quantum-materials-lab", orgId);

        project.Title.Should().Be("Quantum Materials Lab");
        project.OwnerOrganizationId.Should().Be(orgId);
        project.OwnerUserId.Should().BeNull();
    }

    [Fact]
    public void ValidateOwnership_WithBothOwners_ThrowsDomainException()
    {
        var project = new ResearchProject
        {
            Title = "Invalid Dual Owned Project",
            Slug = "invalid-project",
            OwnerUserId = Guid.NewGuid(),
            OwnerOrganizationId = Guid.NewGuid()
        };

        Action act = () => project.ValidateOwnership();

        act.Should().Throw<DomainException>()
            .WithMessage("*must have exactly one owner*");
    }

    [Fact]
    public void ValidateOwnership_WithNoOwners_ThrowsDomainException()
    {
        var project = new ResearchProject
        {
            Title = "Orphan Project",
            Slug = "orphan-project",
            OwnerUserId = null,
            OwnerOrganizationId = null
        };

        Action act = () => project.ValidateOwnership();

        act.Should().Throw<DomainException>()
            .WithMessage("*must have exactly one owner*");
    }
}
