using Pavena.Domain.Common;
using Pavena.Domain.Enums;

namespace Pavena.Domain.Entities.Organizations;

public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public OrganizationType Type { get; set; } = OrganizationType.University;
    public OrganizationVerificationStatus VerificationStatus { get; set; } = OrganizationVerificationStatus.Unverified;
    public string? Description { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string? CountryCode { get; set; }
    public string? City { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    public virtual ICollection<OrganizationMembership> Memberships { get; set; } = new List<OrganizationMembership>();
    public virtual ICollection<OrganizationDepartment> Departments { get; set; } = new List<OrganizationDepartment>();

    public Organization()
    {
    }

    public Organization(string name, string slug, OrganizationType type)
    {
        Name = name;
        Slug = slug.ToLowerInvariant().Trim();
        Type = type;
    }
}
