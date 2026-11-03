using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Pavena.Application.Common.Interfaces;
using Pavena.Domain.Common;
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
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostAttachment> PostAttachments => Set<PostAttachment>();

    public DbSet<ResearchProject> ResearchProjects => Set<ResearchProject>();
    public DbSet<ResearchMember> ResearchMembers => Set<ResearchMember>();
    public DbSet<ResearchTask> ResearchTasks => Set<ResearchTask>();
    public DbSet<ResearchMilestone> ResearchMilestones => Set<ResearchMilestone>();
    public DbSet<ProjectInvitation> ProjectInvitations => Set<ProjectInvitation>();
    public DbSet<ProjectActivityLog> ProjectActivityLogs => Set<ProjectActivityLog>();

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentCollaborator> DocumentCollaborators => Set<DocumentCollaborator>();
    public DbSet<DocumentFile> DocumentFiles => Set<DocumentFile>();
    public DbSet<DocumentCrdtUpdate> DocumentCrdtUpdates => Set<DocumentCrdtUpdate>();
    public DbSet<DocumentSnapshot> DocumentSnapshots => Set<DocumentSnapshot>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<CompilationJob> CompilationJobs => Set<CompilationJob>();
    public DbSet<CompilationArtifact> CompilationArtifacts => Set<CompilationArtifact>();
    public DbSet<DocumentComment> DocumentComments => Set<DocumentComment>();
    public DbSet<DocumentReview> DocumentReviews => Set<DocumentReview>();

    public PavenaDbContext(DbContextOptions<PavenaDbContext> options) : base(options)
    {
        ChangeTracker.Tracked += OnEntityTracked;
        ChangeTracker.StateChanged += OnEntityStateChanged;
    }

    private void OnEntityTracked(object? sender, EntityTrackedEventArgs e)
    {
        if (!e.FromQuery && e.Entry.State == EntityState.Modified && e.Entry.Entity is BaseEntity)
        {
            e.Entry.State = EntityState.Added;
        }
    }

    private void OnEntityStateChanged(object? sender, EntityStateChangedEventArgs e)
    {
        if (e.OldState == EntityState.Detached && e.NewState == EntityState.Modified && e.Entry.Entity is BaseEntity)
        {
            e.Entry.State = EntityState.Added;
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified && entry.Entity is ProjectActivityLog)
            {
                entry.State = EntityState.Added;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
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

        // Adjust provider-specific check constraints if PostgreSQL
        if (Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
        {
            builder.Entity<Connection>().ToTable("connections", t =>
            {
                t.HasCheckConstraint("chk_conn_no_self", "\"RequesterUserId\" <> \"AddresseeUserId\"");
                t.HasCheckConstraint("chk_conn_canonical_order", "\"CanonicalUserA\" < \"CanonicalUserB\"");
            });

            builder.Entity<UserBlock>().ToTable("user_blocks", t =>
            {
                t.HasCheckConstraint("chk_user_blocks_no_self", "\"BlockerUserId\" <> \"BlockedUserId\"");
            });

            builder.Entity<ResearchProject>().ToTable("research_projects", t =>
            {
                t.HasCheckConstraint("chk_rp_single_owner", "(\"OwnerUserId\" IS NOT NULL AND \"OwnerOrganizationId\" IS NULL) OR (\"OwnerUserId\" IS NULL AND \"OwnerOrganizationId\" IS NOT NULL)");
            });
        }
    }
}
