using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pavena.Application.Common.Interfaces;
using Pavena.Domain.Entities.Documents;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Entities.Organizations;
using Pavena.Domain.Entities.Profiles;
using Pavena.Domain.Entities.Research;
using Pavena.Domain.Entities.Social;

namespace Pavena.Infrastructure.Persistence;

public class PavenaDbContext : IdentityDbContext<User, PlatformRole, Guid>, IPavenaDbContext
{
    public DbSet<PlatformRole> PlatformRoles => Roles;
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserEducation> UserEducations => Set<UserEducation>();
    public DbSet<UserExperience> UserExperiences => Set<UserExperience>();
    public DbSet<UserSkill> UserSkills => Set<UserSkill>();
    public DbSet<UserInterest> UserInterests => Set<UserInterest>();
    public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();
    public DbSet<OrganizationDepartment> OrganizationDepartments => Set<OrganizationDepartment>();

    public DbSet<Connection> Connections => Set<Connection>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();

    public DbSet<ResearchProject> ResearchProjects => Set<ResearchProject>();
    public DbSet<ResearchMember> ResearchMembers => Set<ResearchMember>();
    public DbSet<ResearchTask> ResearchTasks => Set<ResearchTask>();

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentCollaborator> DocumentCollaborators => Set<DocumentCollaborator>();

    public PavenaDbContext(DbContextOptions<PavenaDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Customize ASP.NET Identity tables
        builder.Entity<User>().ToTable("users");
        builder.Entity<PlatformRole>().ToTable("platform_roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_platform_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");

        // Apply domain configurations
        builder.ApplyConfigurationsFromAssembly(typeof(PavenaDbContext).Assembly);
    }
}
