using FluentAssertions;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Documents;
using Pavena.Domain.Enums;
using Xunit;

namespace Pavena.UnitTests.Domain;

public class DocumentTests
{
    [Fact]
    public void Create_WithValidParameters_InitializesCorrectly()
    {
        var ownerId = Guid.NewGuid();
        var doc = Document.Create("Deep Learning Foundations", EditorMode.Wysiwyg, ownerId);

        doc.Title.Should().Be("Deep Learning Foundations");
        doc.Mode.Should().Be(EditorMode.Wysiwyg);
        doc.OwnerUserId.Should().Be(ownerId);
        doc.Visibility.Should().Be(DocumentVisibility.Private);
        doc.Status.Should().Be(DocumentStatus.Active);
        doc.SchemaVersion.Should().Be(1);
    }

    [Fact]
    public void Create_WithEmptyTitle_ThrowsDomainException()
    {
        Action act = () => Document.Create("   ", EditorMode.Latex, Guid.NewGuid());

        act.Should().Throw<DomainException>()
            .WithMessage("*title cannot be empty*");
    }

    [Fact]
    public void Create_WithEmptyOwner_ThrowsDomainException()
    {
        Action act = () => Document.Create("Quantum Cryptography", EditorMode.Latex, Guid.Empty);

        act.Should().Throw<DomainException>()
            .WithMessage("*must have a valid owner*");
    }
}
