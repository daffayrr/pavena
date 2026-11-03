using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;

namespace Pavena.Domain.Entities.Profiles;

public class UserEducation : BaseEntity
{
    public Guid UserId { get; set; }
    public string InstitutionName { get; set; } = string.Empty;
    public string? Degree { get; set; }
    public string? FieldOfStudy { get; set; }
    public int? StartYear { get; set; }
    public int? EndYear { get; set; }
    public string? Grade { get; set; }

    public virtual User? User { get; set; }
}

public class UserExperience : BaseEntity
{
    public Guid UserId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string RoleTitle { get; set; } = string.Empty;
    public string? EmploymentType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public string? Description { get; set; }

    public virtual User? User { get; set; }
}

public class UserSkill : BaseEntity
{
    public Guid UserId { get; set; }
    public string SkillName { get; set; } = string.Empty;

    public virtual User? User { get; set; }
}

public class UserInterest : BaseEntity
{
    public Guid UserId { get; set; }
    public string Topic { get; set; } = string.Empty;

    public virtual User? User { get; set; }
}
