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

public enum ChallengeStatus
{
    Draft = 1,
    SubmittedForApproval = 2,
    Approved = 3,
    Published = 4,
    OpenForProposals = 5,
    UnderReview = 6,
    Awarded = 7,
    Closed = 8,
    Cancelled = 9,
    ChangesRequested = 10
}

public enum ChallengeVisibility
{
    Public = 1,
    AuthenticatedUsersOnly = 2,
    InvitationOnly = 3
}

public enum InstitutionalEligibility
{
    OpenToAll = 1,
    UniversitiesOnly = 2,
    AccreditedResearchInstitutesOnly = 3,
    ConsortiumRequired = 4
}

public enum AttachmentConfidentialityLevel
{
    PublicBriefing = 1,
    ConfidentialNdaRequired = 2
}

public enum ChallengeApprovalDecision
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    ChangesRequested = 4
}

public enum RequirementImportance
{
    Required = 1,
    Preferred = 2,
    Optional = 3
}

public enum ProposalStatus
{
    Draft = 1,
    Submitted = 2,
    EligibilityReview = 3,
    UnderReview = 4,
    RevisionRequested = 5,
    Resubmitted = 6,
    Accepted = 7,
    Rejected = 8,
    Withdrawn = 9,
    ConvertedToProject = 10
}

public enum InvestigatorRole
{
    LeadInvestigator = 1,
    CoInvestigator = 2,
    PostdoctoralResearcher = 3,
    GraduateResearcher = 4,
    Consultant = 5
}

public enum ProposalConfidentialityLevel
{
    Confidential = 1,
    RestrictedToSponsorAndReviewers = 2,
    ProprietaryCommercial = 3
}

public enum ProposalAttachmentType
{
    TechnicalProposal = 1,
    BudgetPlan = 2,
    InstitutionalEndorsement = 3,
    PreliminaryData = 4,
    Supplementary = 5
}

public enum ReviewAssignmentStatus
{
    Assigned = 1,
    Accepted = 2,
    Declined = 3,
    Recused = 4,
    Completed = 5
}

public enum AssessmentStatus
{
    Draft = 1,
    Submitted = 2
}

public enum AssessmentRecommendation
{
    RecommendAccept = 1,
    RecommendRevision = 2,
    RecommendReject = 3
}

public enum ProposalActivityType
{
    Created = 1,
    Updated = 2,
    Submitted = 3,
    EligibilityPassed = 4,
    EligibilityFailed = 5,
    ReviewerAssigned = 6,
    ReviewCompleted = 7,
    RevisionRequested = 8,
    Resubmitted = 9,
    Accepted = 10,
    Rejected = 11,
    Withdrawn = 12,
    ConvertedToProject = 13
}

public enum CoiDeclarationType
{
    None = 1,
    InstitutionalProximity = 2,
    PastCollaboration = 3,
    FinancialOrCommercial = 4,
    Other = 5
}



