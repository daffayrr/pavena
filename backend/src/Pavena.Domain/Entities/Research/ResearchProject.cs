using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Entities.Organizations;
using Pavena.Domain.Enums;

namespace Pavena.Domain.Entities.Research;

public class ResearchProject : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Abstract { get; set; }
    public string? ResearchField { get; set; }

    public Guid? OwnerUserId { get; set; }
    public Guid? OwnerOrganizationId { get; set; }

    public ProjectVisibility Visibility { get; set; } = ProjectVisibility.Private;
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    public virtual User? OwnerUser { get; set; }
    public virtual Organization? OwnerOrganization { get; set; }
    public virtual ICollection<ResearchMember> Members { get; set; } = new List<ResearchMember>();
    public virtual ICollection<ResearchTask> Tasks { get; set; } = new List<ResearchTask>();

    public ResearchProject()
    {
    }

    public static ResearchProject CreatePersonal(string title, string slug, Guid ownerUserId)
    {
        var project = new ResearchProject
        {
            Title = title,
            Slug = slug.ToLowerInvariant().Trim(),
            OwnerUserId = ownerUserId,
            OwnerOrganizationId = null,
            Visibility = ProjectVisibility.Private,
            Status = ProjectStatus.Active
        };
        project.ValidateOwnership();
        return project;
    }

    public static ResearchProject CreateInstitutional(string title, string slug, Guid ownerOrganizationId)
    {
        var project = new ResearchProject
        {
            Title = title,
            Slug = slug.ToLowerInvariant().Trim(),
            OwnerUserId = null,
            OwnerOrganizationId = ownerOrganizationId,
            Visibility = ProjectVisibility.Private,
            Status = ProjectStatus.Active
        };
        project.ValidateOwnership();
        return project;
    }

    public void ValidateOwnership()
    {
        bool hasUserOwner = OwnerUserId.HasValue && OwnerUserId.Value != Guid.Empty;
        bool hasOrgOwner = OwnerOrganizationId.HasValue && OwnerOrganizationId.Value != Guid.Empty;

        if (hasUserOwner == hasOrgOwner)
        {
            throw new DomainException("A research project must have exactly one owner: either an individual user or an organization.");
        }
    }
}
