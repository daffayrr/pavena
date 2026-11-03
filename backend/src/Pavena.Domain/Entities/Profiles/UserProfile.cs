using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Enums;

namespace Pavena.Domain.Entities.Profiles;

public class UserProfile
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Headline { get; set; }
    public string? Bio { get; set; }
    public string? CountryCode { get; set; }
    public string? City { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public string? AvatarUrl { get; set; }
    public string? BannerUrl { get; set; }
    public ProfileVisibility Visibility { get; set; } = ProfileVisibility.Public;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    public virtual User? User { get; set; }

    public UserProfile()
    {
    }

    public UserProfile(Guid userId, string displayName)
    {
        UserId = userId;
        DisplayName = displayName;
    }
}
