using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using Pavena.API.Authorization;
using Pavena.API.Health;
using Pavena.API.Hubs;
using Pavena.API.Middleware;
using Pavena.API.Services;
using Pavena.Application;
using Pavena.Application.Identity.Interfaces;
using Pavena.Domain.Entities.Identity;
using Pavena.Infrastructure;
using Pavena.Infrastructure.Persistence;
using Pavena.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Layer Services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 2. HTTP Context & Current User
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// 3. Resource-Based Authorization Handlers
builder.Services.AddScoped<IAuthorizationHandler, ProjectAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, DocumentAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, OrganizationAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, PostAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ChallengeAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ProposalAuthorizationHandler>();

// 4. Policy-Based Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireSuperadmin", policy =>
        policy.RequireRole(PlatformRole.Superadmin));

    options.AddPolicy("RequirePlatformAdmin", policy =>
        policy.RequireRole(PlatformRole.Superadmin, PlatformRole.PlatformAdmin));
});

// 5. Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready", "db" });

// 6. Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsync(
            "{\"type\":\"https://httpstatuses.com/429\",\"title\":\"Too Many Requests\",\"status\":429,\"detail\":\"Rate limit exceeded. Please retry after some time.\"}",
            token);
    };

    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 60;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    options.AddSlidingWindowLimiter("general", opt =>
    {
        opt.PermitLimit = 600;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.SegmentsPerWindow = 6;
        opt.QueueLimit = 10;
    });
});

// 7. Controllers & SignalR & OpenAPI / Swagger
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Pavena Academic Collaboration Platform API",
        Version = "v1",
        Description = "Core REST API for Pavena modular monolith."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 6. Database Seeding on Startup
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

// 7. Explicit CLI Command: Deterministic Demo Data Seeding
if (args.Contains("seed-demo") || args.Contains("--seed-demo"))
{
    using (var scope = app.Services.CreateScope())
    {
        var demoSeeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
        await demoSeeder.SeedAsync();
    }
    return;
}

// 8. HTTP Request Pipeline
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pavena API v1"));
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// 9. Endpoints & Probes
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapControllers();
app.MapHub<CollaborationHub>("/hubs/collaboration");

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }
