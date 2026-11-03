using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Pavena.Application.Challenges.Interfaces;
using Pavena.Application.Common.Interfaces;
using Pavena.Application.Documents.Interfaces;
using Pavena.Application.Identity.Interfaces;
using Pavena.Application.Proposals.Interfaces;
using Pavena.Application.Research.Interfaces;
using Pavena.Application.Social.Interfaces;
using Pavena.Domain.Entities.Identity;
using Pavena.Infrastructure.Challenges;
using Pavena.Infrastructure.Documents;
using Pavena.Infrastructure.Identity;
using Pavena.Infrastructure.Persistence;
using Pavena.Infrastructure.Proposals;
using Pavena.Infrastructure.Research;
using Pavena.Infrastructure.Seeding;
using Pavena.Infrastructure.Social;
using Pavena.Infrastructure.Storage;

namespace Pavena.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Port=3306;Database=pavena_db;User=root;Password=abc_123;CharSet=utf8mb4;";

        var provider = configuration["DatabaseProvider"] ?? "MySQL";

        if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<PavenaDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(PavenaDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                });
                options.ReplaceService<IMigrationsAssembly, ProviderAwareMigrationsAssembly>();
            });
        }
        else
        {
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
            services.AddDbContext<PavenaDbContext>(options =>
            {
                options.UseMySql(connectionString, serverVersion, mySqlOptions =>
                {
                    mySqlOptions.MigrationsAssembly(typeof(PavenaDbContext).Assembly.FullName);
                    mySqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                });
                options.ReplaceService<IMigrationsAssembly, ProviderAwareMigrationsAssembly>();
            });
        }

        services.AddScoped<IPavenaDbContext>(provider => provider.GetRequiredService<PavenaDbContext>());

        // ASP.NET Core Identity Configuration
        services.AddIdentity<User, PlatformRole>(options =>
        {
            // Password policy
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;

            // Lockout policy (Finding F-04)
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User & SignIn policy
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false; // Configurable per environment
        })
        .AddEntityFrameworkStores<PavenaDbContext>()
        .AddDefaultTokenProviders();

        // JWT Authentication Configuration
        var jwtSecret = configuration["Jwt:Secret"] ?? "pavena_super_secret_dev_key_must_be_at_least_32_characters_long_123456";
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "Pavena";
        var jwtAudience = configuration["Jwt:Audience"] ?? "Pavena";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.Zero
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        // Application Service Implementations
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IMalwareScanner, MalwareScanner>();
        services.AddSingleton<IObjectStorageService, PrivateObjectStorageService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IConnectionService, ConnectionService>();
        services.AddScoped<IFollowService, FollowService>();
        services.AddScoped<IBlockService, BlockService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IFeedService, FeedService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentFileSystemService, DocumentFileSystemService>();
        services.AddScoped<IDocumentCollaborationService, DocumentCollaborationService>();
        services.AddScoped<IDocumentVersioningService, DocumentVersioningService>();
        services.AddScoped<ILatexCompilerService, LatexCompilerService>();
        services.AddScoped<IDocumentReviewService, DocumentReviewService>();
        services.AddScoped<IChallengeService, ChallengeService>();
        services.AddScoped<IChallengeAttachmentService, ChallengeAttachmentService>();
        services.AddScoped<IChallengeNdaService, ChallengeNdaService>();
        services.AddScoped<IProposalService, ProposalService>();
        services.AddScoped<IProposalEligibilityService, ProposalEligibilityService>();
        services.AddScoped<IProposalReviewService, ProposalReviewService>();
        services.AddScoped<IProposalConversionService, ProposalConversionService>();
        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<DemoDataSeeder>();

        return services;
    }
}
