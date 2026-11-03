namespace Pavena.Domain.Enums;

public enum UserStatus
{
    Active = 1,
    Suspended = 2,
    Deactivated = 3
}

public enum ProfileVisibility
{
    Public = 1,
    NetworkOnly = 2,
    Private = 3
}

public enum OrganizationType
{
    University = 1,
    School = 2,
    Company = 3,
    ResearchInstitution = 4,
    IndependentResearchOrganization = 5,
    Other = 6
}

public enum OrganizationVerificationStatus
{
    Unverified = 1,
    PendingVerification = 2,
    Verified = 3,
    Rejected = 4
}

public enum OrgMembershipRole
{
    Student = 1,
    Lecturer = 2,
    Teacher = 3,
    Researcher = 4,
    Employee = 5,
    OrganizationAdmin = 6
}

public enum ConnectionStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    Cancelled = 4
}

public enum FollowTargetType
{
    User = 1,
    Organization = 2
}

public enum ProjectVisibility
{
    Private = 1,
    InvitedOnly = 2,
    PublicDiscovery = 3
}

public enum ProjectStatus
{
    Draft = 1,
    Active = 2,
    Completed = 3,
    Archived = 4
}

public enum ResearchRole
{
    Owner = 1,
    ProjectManager = 2,
    Supervisor = 3,
    Member = 4,
    Reviewer = 5,
    Observer = 6
}

public enum TaskPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4
}

public enum TaskStatus
{
    Backlog = 1,
    Todo = 2,
    InProgress = 3,
    InReview = 4,
    Done = 5
}

public enum EditorMode
{
    Wysiwyg = 1,
    Latex = 2
}

public enum DocumentVisibility
{
    Private = 1,
    Shared = 2,
    Public = 3
}

public enum DocumentStatus
{
    Draft = 1,
    Active = 2,
    Archived = 3,
    Trashed = 4
}

public enum DocumentPermissionRole
{
    Owner = 1,
    Editor = 2,
    Reviewer = 3,
    Viewer = 4
}
