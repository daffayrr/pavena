using Microsoft.AspNetCore.Identity;
using Pavena.Domain.Entities.Organizations;
using Pavena.Domain.Entities.Profiles;
using Pavena.Domain.Enums;

namespace Pavena.Domain.Entities.Identity;

public class User : IdentityUser<Guid>
{
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual UserProfile? Profile { get; set; }
    public virtual ICollection<OrganizationMembership> Memberships { get; set; } = new List<OrganizationMembership>();
    public virtual ICollection<UserEducation> Educations { get; set; } = new List<UserEducation>();
    public virtual ICollection<UserExperience> Experiences { get; set; } = new List<UserExperience>();
    public virtual ICollection<UserSkill> Skills { get; set; } = new List<UserSkill>();
    public virtual ICollection<UserInterest> Interests { get; set; } = new List<UserInterest>();
    public virtual ICollection<UserRefreshToken> RefreshTokens { get; set; } = new List<UserRefreshToken>();

    public User()
    {
        Id = Guid.NewGuid();
        SecurityStamp = Guid.NewGuid().ToString();
        ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    public User(string email, string username) : this()
    {
        Email = email;
        NormalizedEmail = email.ToUpperInvariant();
        UserName = username;
        NormalizedUserName = username.ToUpperInvariant();
    }
}
