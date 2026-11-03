using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Pavena.Application.Common.Interfaces;
using Pavena.Domain.Entities.Identity;
using Pavena.Infrastructure.Persistence;
using Pavena.Infrastructure.Seeding;
using Xunit;

namespace Pavena.UnitTests.Seeding;

public class DemoDataSeederUnitTests
{
    [Fact]
    public async Task SeedAsync_WhenEnvironmentIsProduction_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production"
            })
            .Build();

        var mockUserStore = new Mock<IUserStore<User>>();
        var userManager = new UserManager<User>(mockUserStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var mockRoleStore = new Mock<IRoleStore<PlatformRole>>();
        var roleManager = new RoleManager<PlatformRole>(mockRoleStore.Object, null!, null!, null!, null!);

        var dbOptions = new DbContextOptionsBuilder<PavenaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new PavenaDbContext(dbOptions);

        var mockStorage = new Mock<IObjectStorageService>();

        var seeder = new DemoDataSeeder(
            userManager,
            roleManager,
            dbContext,
            mockStorage.Object,
            config,
            NullLogger<DemoDataSeeder>.Instance);

        // Act
        var act = () => seeder.SeedAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Production*");
    }
}
