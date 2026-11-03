using FluentAssertions;
using Pavena.Application.Social.Pagination;
using Pavena.Domain.Common;
using Xunit;

namespace Pavena.UnitTests.Social;

public class FeedCursorTests
{
    [Fact]
    public void EncodeAndDecode_WithValidParameters_ReturnsIdenticalValues()
    {
        var date = DateTime.UtcNow;
        var postId = Guid.NewGuid();

        var encoded = FeedCursorCodec.Encode(date, postId);
        encoded.Should().NotBeNullOrWhiteSpace();

        var decoded = FeedCursorCodec.Decode(encoded);
        decoded.Version.Should().Be(FeedCursorCodec.CurrentVersion);
        decoded.PublishedAtUtc.Ticks.Should().Be(date.Ticks);
        decoded.PostId.Should().Be(postId);
    }

    [Fact]
    public void Decode_WhenEmptyOrWhitespace_ThrowsDomainException()
    {
        Action act1 = () => FeedCursorCodec.Decode(null);
        act1.Should().Throw<DomainException>()
            .WithMessage("*Cursor cannot be empty*");

        Action act2 = () => FeedCursorCodec.Decode("   ");
        act2.Should().Throw<DomainException>()
            .WithMessage("*Cursor cannot be empty*");
    }

    [Fact]
    public void Decode_WhenMalformedBase64_ThrowsDomainException()
    {
        Action act = () => FeedCursorCodec.Decode("not-a-valid-base-64!!!");
        act.Should().Throw<DomainException>()
            .WithMessage("*Malformed feed cursor*");
    }

    [Fact]
    public void Decode_WhenTamperedPayload_ThrowsDomainException()
    {
        var date = DateTime.UtcNow;
        var postId = Guid.NewGuid();
        var validCursor = FeedCursorCodec.Decode(FeedCursorCodec.Encode(date, postId));

        // Create a forged cursor with modified timestamp but original signature
        var forgedPayload = $"1|{date.AddMinutes(-10).Ticks}|{postId:D}|TAMPERED_SIG_1234567890ABCDEF";
        var forgedBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(forgedPayload));

        Action act = () => FeedCursorCodec.Decode(forgedBase64);
        act.Should().Throw<DomainException>()
            .WithMessage("*tampered feed cursor signature*");
    }

    [Fact]
    public void Decode_WhenUnsupportedVersion_ThrowsDomainException()
    {
        var raw = "99|638000000000000000|00000000-0000-0000-0000-000000000000|DUMMY_SIG";
        var b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));

        Action act = () => FeedCursorCodec.Decode(b64);
        act.Should().Throw<DomainException>();
    }
}
