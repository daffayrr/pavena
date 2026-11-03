using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Pavena.Application.Common.Interfaces;
using Pavena.Domain.Entities.Documents;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Entities.Organizations;
using Pavena.Domain.Entities.Research;
using Pavena.Domain.Entities.Social;
using Pavena.Application.Common.Authorization;
using Pavena.Domain.Enums;

namespace Pavena.API.Authorization;

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

        // Platform Superadmin / PlatformAdmin bypass for platform moderation
        if (context.User.IsInRole(PlatformRole.Superadmin) || context.User.IsInRole(PlatformRole.PlatformAdmin))
        {
            context.Succeed(requirement);
            return;
        }

        // Project Owner always has full rights
        if (project.OwnerUserId == userId)
        {
            context.Succeed(requirement);
            return;
        }

        // Institutional project owner: check if user is OrganizationAdmin
        if (project.OwnerOrganizationId.HasValue)
        {
            var isOrgAdmin = await _dbContext.OrganizationMemberships
                .AsNoTracking()
                .AnyAsync(m => m.OrganizationId == project.OwnerOrganizationId.Value &&
                               m.UserId == userId &&
                               m.Role == OrgMembershipRole.OrganizationAdmin &&
                               m.IsActive);

            if (isOrgAdmin)
            {
                context.Succeed(requirement);
                return;
            }
        }

        // Query active member record
        var member = await _dbContext.ResearchMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ProjectId == project.Id && m.UserId == userId && m.Status == ProjectMembershipStatus.Active);

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
            var isLead = member.Role == ProjectMemberRole.ProjectLead ||
                         member.Role == ProjectMemberRole.InstitutionalCoordinator;

            if (requirement.Operation == ResourceOperation.Update && isLead)
            {
                context.Succeed(requirement);
                return;
            }

            if (requirement.Operation == ResourceOperation.ManageMembers && isLead)
            {
                context.Succeed(requirement);
                return;
            }

            if (requirement.Operation == ResourceOperation.Archive && isLead)
            {
                context.Succeed(requirement);
                return;
            }

            if (requirement.Operation == ResourceOperation.Delete && isLead)
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

public class PostAuthorizationHandler : AuthorizationHandler<ResourceOperationRequirement, Post>
{
    private readonly IPavenaDbContext _dbContext;

    public PostAuthorizationHandler(IPavenaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceOperationRequirement requirement,
        Post post)
    {
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        // Deleted posts are not accessible to regular requests
        if (post.IsDeleted)
        {
            return;
        }

        var isSuperadmin = context.User.IsInRole(PlatformRole.Superadmin);
        var isPlatformAdmin = context.User.IsInRole(PlatformRole.PlatformAdmin);

        // Author always has full access
        if (post.AuthorUserId == userId)
        {
            context.Succeed(requirement);
            return;
        }

        // Only author can update, publish, or archive
        if (requirement.Operation == ResourceOperation.Update ||
            requirement.Operation == ResourceOperation.Publish ||
            requirement.Operation == ResourceOperation.Archive)
        {
            return;
        }

        // Delete can be performed by Platform Moderation
        if (requirement.Operation == ResourceOperation.Delete)
        {
            if (isSuperadmin || isPlatformAdmin)
            {
                context.Succeed(requirement);
            }
            return;
        }

        // View (Read) authorization rules
        if (requirement.Operation == ResourceOperation.Read)
        {
            // Draft and Archived posts are strictly private to the author
            if (post.Status == PostStatus.Draft || post.Status == PostStatus.Archived)
            {
                return;
            }

            // Platform moderation can inspect published posts
            if (isSuperadmin)
            {
                context.Succeed(requirement);
                return;
            }

            // Bilateral block check: If either user blocked the other, view is denied
            var isBlocked = await _dbContext.UserBlocks
                .AsNoTracking()
                .AnyAsync(b => (b.BlockerUserId == post.AuthorUserId && b.BlockedUserId == userId) ||
                               (b.BlockerUserId == userId && b.BlockedUserId == post.AuthorUserId));
            if (isBlocked)
            {
                return;
            }

            // OnlyMe is strictly author-only
            if (post.Visibility == PostVisibility.OnlyMe)
            {
                return;
            }

            // Public visibility: accessible to authenticated non-blocked users
            if (post.Visibility == PostVisibility.Public)
            {
                context.Succeed(requirement);
                return;
            }

            // Connections visibility: accessible ONLY if mutual accepted connection exists
            if (post.Visibility == PostVisibility.Connections)
            {
                var (canonA, canonB) = userId.CompareTo(post.AuthorUserId) < 0
                    ? (userId, post.AuthorUserId)
                    : (post.AuthorUserId, userId);

                var isConnected = await _dbContext.Connections
                    .AsNoTracking()
                    .AnyAsync(c => c.CanonicalUserA == canonA &&
                                   c.CanonicalUserB == canonB &&
                                   c.Status == ConnectionStatus.Accepted);

                if (isConnected)
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}
