using System.Security.Cryptography;
using System.Text;
using Pavena.Domain.Common;

namespace Pavena.Application.Social.Pagination;

public record FeedCursor(
    int Version,
    DateTime PublishedAtUtc,
    Guid PostId
);

public static class FeedCursorCodec
{
    public const int CurrentVersion = 1;
    private static readonly byte[] SecretKey = Encoding.UTF8.GetBytes("Pavena_Deterministic_Feed_Cursor_Secret_2026_Signing_Key_128bit");

    public static string Encode(DateTime publishedAtUtc, Guid postId)
    {
        var utc = publishedAtUtc.Kind switch
        {
            DateTimeKind.Utc => publishedAtUtc,
            DateTimeKind.Local => publishedAtUtc.ToUniversalTime(),
            _ => DateTime.SpecifyKind(publishedAtUtc, DateTimeKind.Utc)
        };
        var payload = $"{CurrentVersion}|{utc.Ticks}|{postId:D}";
        var signature = ComputeSignature(payload);
        var combined = $"{payload}|{signature}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(combined))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static FeedCursor Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            throw new DomainException("Cursor cannot be empty.");

        string decoded;
        try
        {
            var base64 = cursor.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }
        catch (Exception)
        {
            throw new DomainException("Malformed feed cursor.");
        }

        var parts = decoded.Split('|');
        if (parts.Length != 4)
            throw new DomainException("Malformed feed cursor format.");

        if (!int.TryParse(parts[0], out var version) || version != CurrentVersion)
            throw new DomainException($"Unsupported cursor version '{parts[0]}'.");

        var payload = $"{parts[0]}|{parts[1]}|{parts[2]}";
        var expectedSignature = ComputeSignature(payload);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(parts[3]),
                Encoding.UTF8.GetBytes(expectedSignature)))
        {
            throw new DomainException("Invalid or tampered feed cursor signature.");
        }

        if (!long.TryParse(parts[1], out var ticks))
            throw new DomainException("Invalid timestamp in feed cursor.");

        DateTime publishedAtUtc;
        try
        {
            publishedAtUtc = new DateTime(ticks, DateTimeKind.Utc);
        }
        catch (Exception)
        {
            throw new DomainException("Invalid datetime bounds in feed cursor.");
        }

        if (!Guid.TryParse(parts[2], out var postId))
            throw new DomainException("Invalid post ID in feed cursor.");

        return new FeedCursor(version, publishedAtUtc, postId);
    }

    private static string ComputeSignature(string payload)
    {
        using var hmac = new HMACSHA256(SecretKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }
}
