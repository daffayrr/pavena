using Microsoft.EntityFrameworkCore;
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

    DbSet<Document> Documents { get; }
    DbSet<DocumentCollaborator> DocumentCollaborators { get; }

    Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
