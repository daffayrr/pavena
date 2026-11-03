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

public enum ProjectOriginType
{
    ResearcherInitiated = 1,
    IndustryChallenge = 2,
    InstitutionalInitiated = 3
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
    Archived = 4,
    Planning = 5,
    OnHold = 6
}

public enum ResearchStage
{
    Concept = 1,
    MethodologyDesign = 2,
    DataCollection = 3,
    Analysis = 4,
    Review = 5,
    IndustryPitchReady = 6,
    Translation = 7
}

public enum ProjectMemberRole
{
    ProjectLead = 1,
    Researcher = 2,
    CoResearcher = 3,
    ResearchAssistant = 4,
    InstitutionalCoordinator = 5,
    IndustryPartner = 6,
    Observer = 7
}

public enum ProjectMembershipStatus
{
    Invited = 1,
    Active = 2,
    Declined = 3,
    Removed = 4,
    Left = 5
}

public enum ProjectInvitationStatus
{
    Pending = 1,
    Accepted = 2,
    Declined = 3,
    Revoked = 4,
    Expired = 5
}

public enum MilestoneStatus
{
    Planned = 1,
    InProgress = 2,
    Achieved = 3,
    Deferred = 4,
    Cancelled = 5
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

public enum ResearchTaskStatus
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

public enum PostVisibility
{
    Public = 1,
    Connections = 2,
    OnlyMe = 3
}

public enum PostStatus
{
    Draft = 1,
    Published = 2,
    Archived = 3
}

public enum PostCategory
{
    ResearchUpdate = 1,
    ResearchFinding = 2,
    AcademicDiscussion = 3,
    Publication = 4,
    CollaborationOpportunity = 5,
    ConferenceAndEvent = 6,
    GeneralAcademic = 7
}

public enum CompilationStatus
{
    Queued = 1,
    Running = 2,
    Succeeded = 3,
    Failed = 4,
    TimedOut = 5
}

public enum CompilationEngine
{
    PdfLatex = 1,
    XeLatex = 2,
    LuaLatex = 3
}

public enum CompilationArtifactType
{
    Pdf = 1,
    Log = 2,
    Synctex = 3,
    Aux = 4
}

public enum DocumentVersionSource
{
    ManualSnapshot = 1,
    AutoSave = 2,
    CompilationSuccess = 3,
    Release = 4
}

public enum ReviewStatus
{
    Pending = 1,
    Approved = 2,
    ChangesRequested = 3,
    Rejected = 4
}

public enum CommentStatus
{
    Active = 1,
    Resolved = 2
}

