using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Enums;

namespace Pavena.Domain.Entities.Organizations;

public class OrganizationMembership : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public OrgMembershipRole Role { get; set; } = OrgMembershipRole.Student;
    public string? Department { get; set; }
    public string? Title { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public virtual Organization? Organization { get; set; }
    public virtual User? User { get; set; }

    public OrganizationMembership()
    {
    }

    public OrganizationMembership(Guid organizationId, Guid userId, OrgMembershipRole role)
    {
        OrganizationId = organizationId;
        UserId = userId;
        Role = role;
    }
}

public class OrganizationDepartment : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }

    public virtual Organization? Organization { get; set; }
}
