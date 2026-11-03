using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pavena.Domain.Entities.Challenges;
using Pavena.Domain.Entities.Documents;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Entities.Organizations;
using Pavena.Domain.Entities.Profiles;
using Pavena.Domain.Entities.Research;
using Pavena.Domain.Entities.Social;

namespace Pavena.Application.Common.Interfaces;

public interface IPavenaDbContext
{
    DbSet<User> Users { get; }
    DbSet<PlatformRole> PlatformRoles { get; }
    DbSet<IdentityUserRole<Guid>> UserPlatformRoles { get; }
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<UserEducation> UserEducations { get; }
    DbSet<UserExperience> UserExperiences { get; }
    DbSet<UserSkill> UserSkills { get; }
    DbSet<UserInterest> UserInterests { get; }
    DbSet<UserRefreshToken> UserRefreshTokens { get; }
    DbSet<AdminAuditLog> AdminAuditLogs { get; }

    DbSet<Organization> Organizations { get; }
    DbSet<OrganizationMembership> OrganizationMemberships { get; }
    DbSet<OrganizationDepartment> OrganizationDepartments { get; }

    DbSet<Connection> Connections { get; }
    DbSet<Follow> Follows { get; }
    DbSet<UserBlock> UserBlocks { get; }
    DbSet<Post> Posts { get; }
    DbSet<PostAttachment> PostAttachments { get; }

    DbSet<ResearchProject> ResearchProjects { get; }
    DbSet<ResearchMember> ResearchMembers { get; }
    DbSet<ResearchTask> ResearchTasks { get; }
    DbSet<ResearchMilestone> ResearchMilestones { get; }
    DbSet<ProjectInvitation> ProjectInvitations { get; }
    DbSet<ProjectActivityLog> ProjectActivityLogs { get; }

    DbSet<Document> Documents { get; }
    DbSet<DocumentCollaborator> DocumentCollaborators { get; }
    DbSet<DocumentFile> DocumentFiles { get; }
    DbSet<DocumentCrdtUpdate> DocumentCrdtUpdates { get; }
    DbSet<DocumentSnapshot> DocumentSnapshots { get; }
    DbSet<DocumentVersion> DocumentVersions { get; }
    DbSet<CompilationJob> CompilationJobs { get; }
    DbSet<CompilationArtifact> CompilationArtifacts { get; }
    DbSet<DocumentComment> DocumentComments { get; }
    DbSet<DocumentReview> DocumentReviews { get; }

    DbSet<IndustryChallenge> IndustryChallenges { get; }
    DbSet<ChallengeAttachment> ChallengeAttachments { get; }
    DbSet<ChallengeResearchDomain> ChallengeResearchDomains { get; }
    DbSet<ChallengeExpertiseRequirement> ChallengeExpertiseRequirements { get; }
    DbSet<ChallengeApproval> ChallengeApprovals { get; }
    DbSet<ChallengeActivityLog> ChallengeActivityLogs { get; }
    DbSet<ChallengeNdaAgreement> ChallengeNdaAgreements { get; }

    Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
