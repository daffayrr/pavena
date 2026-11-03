using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Entities.Organizations;
using Pavena.Domain.Entities.Research;
using Pavena.Domain.Entities.Social;
using Pavena.Domain.Enums;
using Pavena.Infrastructure.Persistence;
using Xunit;

namespace Pavena.IntegrationTests.Persistence;

public class DatabaseConstraintIntegrationTests
{
    private static PavenaDbContext CreateMySqlTestDbContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost;Port=3306;Database=pavena_test_db;User=root;Password=abc_123;CharSet=utf8mb4;";

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
        var options = new DbContextOptionsBuilder<PavenaDbContext>()
            .UseMySql(connectionString, serverVersion)
            .Options;

        return new PavenaDbContext(options);
    }

    [Fact]
    public async Task MySQL_Enforces_CanonicalOrderCheckConstraint()
    {
        using var db = CreateMySqlTestDbContext();

        var user1 = new User("user1_" + Guid.NewGuid().ToString("N")[..6] + "@pavena.org", "u1_" + Guid.NewGuid().ToString("N")[..6]) { PasswordHash = "hash1" };
        var user2 = new User("user2_" + Guid.NewGuid().ToString("N")[..6] + "@pavena.org", "u2_" + Guid.NewGuid().ToString("N")[..6]) { PasswordHash = "hash2" };
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        var u1 = user1.Id;
        var u2 = user2.Id;
        if (u1.CompareTo(u2) > 0)
        {
            (u1, u2) = (u2, u1);
        }

        // Attempt to violate constraint: canonical_user_a >= canonical_user_b
        var invalidConnection = new Connection
        {
            RequesterUserId = u2,
            AddresseeUserId = u1,
            CanonicalUserA = u2, // INVALID: u2 > u1
            CanonicalUserB = u1,
            Status = ConnectionStatus.Pending,
            RequestedAtUtc = DateTime.UtcNow
        };

        db.Connections.Add(invalidConnection);

        Func<Task> act = async () => await db.SaveChangesAsync();

        // Must be rejected by MySQL check constraint chk_conn_canonical_order
        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        ex.Which.InnerException.Should().NotBeNull();
        ex.Which.InnerException!.Message.Should().Contain("chk_conn_canonical_order");
    }

    [Fact]
    public async Task MySQL_Enforces_SingleProjectOwnerCheckConstraint()
    {
        using var db = CreateMySqlTestDbContext();

        var user = new User("user_" + Guid.NewGuid().ToString("N")[..6] + "@pavena.org", "u_" + Guid.NewGuid().ToString("N")[..6]) { PasswordHash = "hash" };
        var org = new Organization("Test Org " + Guid.NewGuid().ToString("N")[..6], "test-org-" + Guid.NewGuid().ToString("N")[..6], OrganizationType.University);
        db.Users.Add(user);
        db.Organizations.Add(org);
        await db.SaveChangesAsync();

        // Attempt to violate constraint: both owners non-null
        var invalidProject = new ResearchProject
        {
            Title = "Dual Owned Test Project",
            Slug = "dual-owned-" + Guid.NewGuid().ToString("N")[..8],
            OwnerUserId = user.Id,
            OwnerOrganizationId = org.Id,
            Status = ProjectStatus.Active,
            Visibility = ProjectVisibility.Private
        };

        db.ResearchProjects.Add(invalidProject);

        Func<Task> act = async () => await db.SaveChangesAsync();

        // Must be rejected by MySQL check constraint chk_rp_single_owner
        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        ex.Which.InnerException.Should().NotBeNull();
        ex.Which.InnerException!.Message.Should().Contain("chk_rp_single_owner");
    }

    [Fact]
    public async Task MySQL_Enforces_UniqueNormalizedEmail()
    {
        using var db = CreateMySqlTestDbContext();

        var email = $"test_{Guid.NewGuid():N}@pavena.org";

        var user1 = new User(email, "user1_" + Guid.NewGuid().ToString("N")[..6])
        {
            PasswordHash = "hash1"
        };
        db.Users.Add(user1);
        await db.SaveChangesAsync();

        // Second user with same email (different casing)
        var user2 = new User(email.ToUpperInvariant(), "user2_" + Guid.NewGuid().ToString("N")[..6])
        {
            PasswordHash = "hash2"
        };
        db.Users.Add(user2);

        Func<Task> act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
