using Microsoft.AspNetCore.Authorization;

namespace Pavena.Application.Common.Authorization;

public enum ResourceOperation
{
    Read,
    Create,
    Update,
    Delete,
    ManageMembers,
    ManageCollaborators,
    Publish,
    Archive
}

public class ResourceOperationRequirement : IAuthorizationRequirement
{
    public ResourceOperation Operation { get; }

    public ResourceOperationRequirement(ResourceOperation operation)
    {
        Operation = operation;
    }
}
