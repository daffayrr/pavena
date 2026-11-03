using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Pavena.API.Authorization;
using Pavena.Application.Common.Authorization;
using Pavena.Domain.Entities.Documents;
using Pavena.Domain.Entities.Research;
using Pavena.Domain.Enums;
using Pavena.Infrastructure.Persistence;
using Xunit;

namespace Pavena.UnitTests.Authorization;

public class ResourceAuthorizationHandlerTests
{
    private static PavenaDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<PavenaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new PavenaDbContext(options);
    }

    [Fact]
    public async Task DocumentHandler_WhenUserIsOwner_AuthorizesAllOperations()
    {
        using var db = CreateInMemoryDbContext();
        var handler = new DocumentAuthorizationHandler(db);

        var ownerId = Guid.NewGuid();
        var document = Document.Create("My Research Thesis", EditorMode.Wysiwyg, ownerId);

        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, ownerId.ToString())
        ], "TestAuth"));

        var contextRead = new AuthorizationHandlerContext(
            [new ResourceOperationRequirement(ResourceOperation.Read)],
            user,
            document
        );

        var contextUpdate = new AuthorizationHandlerContext(
            [new ResourceOperationRequirement(ResourceOperation.Update)],
            user,
            document
        );

        var contextManage = new AuthorizationHandlerContext(
            [new ResourceOperationRequirement(ResourceOperation.ManageCollaborators)],
            user,
            document
        );

        await handler.HandleAsync(contextRead);
        await handler.HandleAsync(contextUpdate);
        await handler.HandleAsync(contextManage);

        contextRead.HasSucceeded.Should().BeTrue();
        contextUpdate.HasSucceeded.Should().BeTrue();
        contextManage.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task DocumentHandler_WhenUserIsEditor_AuthorizesReadAndUpdate_DeniesManageCollaborators()
    {
        using var db = CreateInMemoryDbContext();
        var handler = new DocumentAuthorizationHandler(db);

        var ownerId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var document = Document.Create("Shared Paper", EditorMode.Latex, ownerId);

        var collaborator = new DocumentCollaborator(document.Id, editorId, DocumentPermissionRole.Editor, ownerId);
        db.DocumentCollaborators.Add(collaborator);
        await db.SaveChangesAsync();

        var editorUser = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, editorId.ToString())
        ], "TestAuth"));

        var contextRead = new AuthorizationHandlerContext(
            [new ResourceOperationRequirement(ResourceOperation.Read)],
            editorUser,
            document
        );

        var contextUpdate = new AuthorizationHandlerContext(
            [new ResourceOperationRequirement(ResourceOperation.Update)],
            editorUser,
            document
        );

        var contextManage = new AuthorizationHandlerContext(
            [new ResourceOperationRequirement(ResourceOperation.ManageCollaborators)],
            editorUser,
            document
        );

        await handler.HandleAsync(contextRead);
        await handler.HandleAsync(contextUpdate);
        await handler.HandleAsync(contextManage);

        contextRead.HasSucceeded.Should().BeTrue();
        contextUpdate.HasSucceeded.Should().BeTrue();
        contextManage.HasSucceeded.Should().BeFalse(); // Editor cannot manage collaborators
    }

    [Fact]
    public async Task DocumentHandler_WhenUserIsProjectMemberOnly_DeniesPrivateDocumentAccess()
    {
        using var db = CreateInMemoryDbContext();
        var handler = new DocumentAuthorizationHandler(db);

        var ownerId = Guid.NewGuid();
        var projectMemberId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        // Private document associated with project
        var document = Document.Create("Confidential Draft", EditorMode.Wysiwyg, ownerId, projectId);
        document.Visibility = DocumentVisibility.Private;

        // User is member of the project, but NOT explicitly a document collaborator
        var projectMember = new ResearchMember(projectId, projectMemberId, ResearchRole.Member, ownerId);
        db.ResearchMembers.Add(projectMember);
        await db.SaveChangesAsync();

        var memberUser = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, projectMemberId.ToString())
        ], "TestAuth"));

        var context = new AuthorizationHandlerContext(
            [new ResourceOperationRequirement(ResourceOperation.Read)],
            memberUser,
            document
        );

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse("project membership alone must never bypass document permissions");
    }
}
