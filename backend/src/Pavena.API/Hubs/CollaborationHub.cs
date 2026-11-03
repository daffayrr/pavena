using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Pavena.Application.Documents.Interfaces;
using Pavena.Domain.Enums;

namespace Pavena.API.Hubs;

[Authorize]
public class CollaborationHub : Hub
{
    private const int MaxPayloadSizeBytes = 512 * 1024; // 512 KB limit per frame

    // ConnectionId -> Set of DocumentIds
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, byte>> ConnectionRooms = new();

    private readonly IDocumentCollaborationService _collaborationService;
    private readonly ILogger<CollaborationHub> _logger;

    public CollaborationHub(
        IDocumentCollaborationService collaborationService,
        ILogger<CollaborationHub> logger)
    {
        _collaborationService = collaborationService;
        _logger = logger;
    }

    public async Task JoinDocument(Guid documentId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            throw new HubException("Unauthenticated connection.");

        var hasAccess = await _collaborationService.CanUserAccessDocumentAsync(userId.Value, documentId, DocumentPermissionRole.Viewer);
        if (!hasAccess)
        {
            _logger.LogWarning("User {UserId} unauthorized to join document {DocumentId}.", userId, documentId);
            throw new HubException("You do not have permission to access this document room.");
        }

        var groupName = GetDocumentGroupName(documentId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        var rooms = ConnectionRooms.GetOrAdd(Context.ConnectionId, _ => new ConcurrentDictionary<Guid, byte>());
        rooms.TryAdd(documentId, 0);

        // Fetch current aggregated CRDT state vector
        var stateVector = await _collaborationService.GetAggregatedCrdtStateAsync(userId.Value, documentId, null);

        // Send initialization state back to newly joined caller
        await Clients.Caller.SendAsync("SyncStep1", new
        {
            documentId,
            state = stateVector
        });

        // Broadcast presence to other collaborators in room
        await Clients.OthersInGroup(groupName).SendAsync("UserJoined", new
        {
            documentId,
            userId = userId.Value,
            connectionId = Context.ConnectionId
        });

        _logger.LogInformation("Connection {ConnectionId} (User {UserId}) joined document room {DocumentId}.",
            Context.ConnectionId, userId, documentId);
    }

    public async Task LeaveDocument(Guid documentId)
    {
        var userId = GetCurrentUserId();
        var groupName = GetDocumentGroupName(documentId);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        if (ConnectionRooms.TryGetValue(Context.ConnectionId, out var rooms))
        {
            rooms.TryRemove(documentId, out _);
        }

        await Clients.OthersInGroup(groupName).SendAsync("UserLeft", new
        {
            documentId,
            userId = userId ?? Guid.Empty,
            connectionId = Context.ConnectionId
        });

        _logger.LogInformation("Connection {ConnectionId} left document room {DocumentId}.", Context.ConnectionId, documentId);
    }

    public async Task SendUpdate(Guid documentId, Guid? fileId, byte[] updateBlob)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            throw new HubException("Unauthenticated connection.");

        if (updateBlob == null || updateBlob.Length == 0)
            return;

        if (updateBlob.Length > MaxPayloadSizeBytes)
            throw new HubException($"CRDT update payload exceeds maximum permitted size of {MaxPayloadSizeBytes / 1024} KB.");

        var canEdit = await _collaborationService.CanUserAccessDocumentAsync(userId.Value, documentId, DocumentPermissionRole.Editor);
        if (!canEdit)
        {
            throw new HubException("You do not have write access to this document.");
        }

        // Persist update durably
        await _collaborationService.RecordCrdtUpdateAsync(userId.Value, documentId, fileId, updateBlob, Context.ConnectionId);

        // Broadcast frame to other active peers
        var groupName = GetDocumentGroupName(documentId);
        await Clients.OthersInGroup(groupName).SendAsync("ReceiveUpdate", new
        {
            documentId,
            fileId,
            updateBlob,
            senderUserId = userId.Value,
            senderConnectionId = Context.ConnectionId
        });
    }

    public async Task UpdateAwareness(Guid documentId, byte[] awarenessBlob)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return;

        if (awarenessBlob == null || awarenessBlob.Length > MaxPayloadSizeBytes)
            return;

        var groupName = GetDocumentGroupName(documentId);
        await Clients.OthersInGroup(groupName).SendAsync("ReceiveAwareness", new
        {
            documentId,
            awarenessBlob,
            senderUserId = userId.Value,
            senderConnectionId = Context.ConnectionId
        });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();

        if (ConnectionRooms.TryRemove(Context.ConnectionId, out var rooms))
        {
            foreach (var docId in rooms.Keys)
            {
                var groupName = GetDocumentGroupName(docId);
                await Clients.OthersInGroup(groupName).SendAsync("UserLeft", new
                {
                    documentId = docId,
                    userId = userId ?? Guid.Empty,
                    connectionId = Context.ConnectionId
                });
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? Context.User?.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static string GetDocumentGroupName(Guid documentId) => $"doc_{documentId}";
}
