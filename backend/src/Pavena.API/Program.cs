using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Pavena.API.Authorization;
using Pavena.API.Middleware;
using Pavena.API.Services;
using Pavena.Application;
using Pavena.Application.Identity.Interfaces;
using Pavena.Domain.Entities.Identity;
using Pavena.Infrastructure;
using Pavena.Infrastructure.Persistence;

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

// 4. Policy-Based Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireSuperadmin", policy =>
        policy.RequireRole(PlatformRole.Superadmin));

    options.AddPolicy("RequirePlatformAdmin", policy =>
        policy.RequireRole(PlatformRole.Superadmin, PlatformRole.PlatformAdmin));
});

// 5. Controllers & OpenAPI / Swagger
builder.Services.AddControllers();
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

// 7. HTTP Request Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pavena API v1"));
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }
