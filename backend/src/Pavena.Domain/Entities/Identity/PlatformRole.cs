using Microsoft.AspNetCore.Identity;

namespace Pavena.Domain.Entities.Identity;

public class PlatformRole : IdentityRole<Guid>
{
    public const string Superadmin = "Superadmin";
    public const string PlatformAdmin = "PlatformAdmin";

    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public PlatformRole()
    {
        Id = Guid.NewGuid();
        ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    public PlatformRole(string roleName, string? description = null) : this()
    {
        Name = roleName;
        NormalizedName = roleName.ToUpperInvariant();
        Description = description;
    }
}
