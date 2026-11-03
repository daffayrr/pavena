using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Pavena.Application.Common.Interfaces;
using Pavena.Domain.Entities.Documents;
using Pavena.Domain.Entities.Organizations;
using Pavena.Domain.Entities.Research;
using Pavena.Domain.Enums;

namespace Pavena.API.Authorization;

public enum ResourceOperation
{
    Read,
    Create,
    Update,
    Delete,
    ManageMembers,
    ManageCollaborators
}

public class ResourceOperationRequirement : IAuthorizationRequirement
{
    public ResourceOperation Operation { get; }

    public ResourceOperationRequirement(ResourceOperation operation)
    {
        Operation = operation;
    }
}

public class ProjectAuthorizationHandler : AuthorizationHandler<ResourceOperationRequirement, ResearchProject>
{
    private readonly IPavenaDbContext _dbContext;

    public ProjectAuthorizationHandler(IPavenaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceOperationRequirement requirement,
        ResearchProject project)
    {
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        // Project Owner always has full rights
        if (project.OwnerUserId == userId)
        {
            context.Succeed(requirement);
            return;
        }

        // Query member record
        var member = await _dbContext.ResearchMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ProjectId == project.Id && m.UserId == userId);

        if (requirement.Operation == ResourceOperation.Read)
        {
            if (project.Visibility == ProjectVisibility.PublicDiscovery || member != null)
            {
                context.Succeed(requirement);
                return;
            }
        }

        if (member != null)
        {
            if (requirement.Operation == ResourceOperation.Update &&
                (member.Role == ResearchRole.Owner || member.Role == ResearchRole.ProjectManager))
            {
                context.Succeed(requirement);
                return;
            }

            if (requirement.Operation == ResourceOperation.ManageMembers &&
                (member.Role == ResearchRole.Owner || member.Role == ResearchRole.ProjectManager))
            {
                context.Succeed(requirement);
                return;
            }
        }
    }
}

public class DocumentAuthorizationHandler : AuthorizationHandler<ResourceOperationRequirement, Document>
{
    private readonly IPavenaDbContext _dbContext;

    public DocumentAuthorizationHandler(IPavenaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceOperationRequirement requirement,
        Document document)
    {
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        // Document Owner has full rights
        if (document.OwnerUserId == userId)
        {
            context.Succeed(requirement);
            return;
        }

        // Explicit collaborator check (Project membership alone DOES NOT grant access)
        var collaborator = await _dbContext.DocumentCollaborators
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.DocumentId == document.Id && c.UserId == userId);

        if (requirement.Operation == ResourceOperation.Read)
        {
            if (document.Visibility == DocumentVisibility.Public || collaborator != null)
            {
                context.Succeed(requirement);
                return;
            }
        }

        if (collaborator != null)
        {
            if (requirement.Operation == ResourceOperation.Update &&
                (collaborator.Role == DocumentPermissionRole.Owner || collaborator.Role == DocumentPermissionRole.Editor))
            {
                context.Succeed(requirement);
                return;
            }

            if (requirement.Operation == ResourceOperation.ManageCollaborators &&
                collaborator.Role == DocumentPermissionRole.Owner)
            {
                context.Succeed(requirement);
                return;
            }
        }
    }
}

public class OrganizationAuthorizationHandler : AuthorizationHandler<ResourceOperationRequirement, Organization>
{
    private readonly IPavenaDbContext _dbContext;

    public OrganizationAuthorizationHandler(IPavenaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceOperationRequirement requirement,
        Organization organization)
    {
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        if (requirement.Operation == ResourceOperation.Read)
        {
            context.Succeed(requirement);
            return;
        }

        var membership = await _dbContext.OrganizationMemberships
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.OrganizationId == organization.Id && m.UserId == userId && m.IsActive);

        if (membership != null && membership.Role == OrgMembershipRole.OrganizationAdmin)
        {
            context.Succeed(requirement);
        }
    }
}
