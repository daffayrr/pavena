using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Entities.Profiles;
using Pavena.Domain.Enums;

namespace Pavena.Infrastructure.Persistence;

public class DatabaseSeeder
{
    private readonly RoleManager<PlatformRole> _roleManager;
    private readonly UserManager<User> _userManager;
    private readonly PavenaDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        RoleManager<PlatformRole> roleManager,
        UserManager<User> userManager,
        PavenaDbContext dbContext,
        IConfiguration configuration,
        ILogger<DatabaseSeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        // 0. Ensure Pending Migrations are applied
        await _dbContext.Database.MigrateAsync();

        // 1. Seed Platform Roles
        string[] roles = [PlatformRole.Superadmin, PlatformRole.PlatformAdmin];

        foreach (var roleName in roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var role = new PlatformRole(roleName, $"Platform Governance Role: {roleName}");
                await _roleManager.CreateAsync(role);
                _logger.LogInformation("Seeded platform role: {RoleName}", roleName);
            }
        }

        // 2. Seed Development Superadmin (Environment-based, never hardcoded production password)
        var superadminEmail = _configuration["SUPERADMIN_EMAIL"] ?? "superadmin@pavena.local";
        var superadminPassword = _configuration["SUPERADMIN_PASSWORD"] ?? "PavenaAdmin#2026";

        var existingSuperadmin = await _userManager.FindByEmailAsync(superadminEmail)
            ?? await _userManager.FindByNameAsync("superadmin");
        if (existingSuperadmin == null)
        {
            var adminUser = new User(superadminEmail, "superadmin")
            {
                EmailConfirmed = true,
                Status = UserStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(adminUser, superadminPassword);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, PlatformRole.Superadmin);

                var profile = new UserProfile(adminUser.Id, "Platform Superadministrator")
                {
                    Headline = "Chief Platform Governance Officer",
                    Bio = "System Administrator for Pavena Global Platform.",
                    PreferredLanguage = "en",
                    Visibility = ProfileVisibility.Public
                };

                _dbContext.UserProfiles.Add(profile);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Bootstrapped initial Superadmin account: {Email}", superadminEmail);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to bootstrap superadmin: {Errors}", errors);
            }
        }
        else
        {
            if (!await _userManager.IsInRoleAsync(existingSuperadmin, PlatformRole.Superadmin))
            {
                await _userManager.AddToRoleAsync(existingSuperadmin, PlatformRole.Superadmin);
                _logger.LogInformation("Assigned Superadmin role to existing account: {Email}", existingSuperadmin.Email);
            }
        }
    }
}
