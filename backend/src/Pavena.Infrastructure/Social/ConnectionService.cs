using Microsoft.EntityFrameworkCore;
using Pavena.Application.Common.Interfaces;
using Pavena.Application.Social.DTOs;
using Pavena.Application.Social.Interfaces;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Social;
using Pavena.Domain.Enums;

namespace Pavena.Infrastructure.Social;

public class ConnectionService : IConnectionService
{
    private readonly IPavenaDbContext _dbContext;

    public ConnectionService(IPavenaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ConnectionDto> SendRequestAsync(Guid requesterId, Guid addresseeId, CancellationToken cancellationToken = default)
    {
        if (requesterId == addresseeId)
            throw new DomainException("A user cannot send a connection request to themselves.");

        var addresseeExists = await _dbContext.Users
            .AnyAsync(u => u.Id == addresseeId, cancellationToken);
        if (!addresseeExists)
            throw new DomainException("Target user does not exist.");

        // Check if blocked in either direction
        var isBlocked = await _dbContext.UserBlocks
            .AnyAsync(b => (b.BlockerUserId == requesterId && b.BlockedUserId == addresseeId) ||
                           (b.BlockerUserId == addresseeId && b.BlockedUserId == requesterId), cancellationToken);
        if (isBlocked)
            throw new DomainException("Cannot send a connection request to this user.");

        var (canonA, canonB) = requesterId.CompareTo(addresseeId) < 0
            ? (requesterId, addresseeId)
            : (addresseeId, requesterId);

        var existing = await _dbContext.Connections
            .FirstOrDefaultAsync(c => c.CanonicalUserA == canonA && c.CanonicalUserB == canonB, cancellationToken);

        Connection connection;
        if (existing == null)
        {
            connection = Connection.CreateRequest(requesterId, addresseeId);
            _dbContext.Connections.Add(connection);
        }
        else
        {
            existing.ReRequest(requesterId, addresseeId);
            connection = existing;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(connection);
    }

    public async Task<ConnectionDto> AcceptRequestAsync(Guid callerUserId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        var connection = await _dbContext.Connections
            .FirstOrDefaultAsync(c => c.Id == connectionId, cancellationToken);

        if (connection == null)
            throw new DomainException("Connection request not found.");

        connection.Accept(callerUserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(connection);
    }

    public async Task<ConnectionDto> RejectRequestAsync(Guid callerUserId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        var connection = await _dbContext.Connections
            .FirstOrDefaultAsync(c => c.Id == connectionId, cancellationToken);

        if (connection == null)
            throw new DomainException("Connection request not found.");

        connection.Reject(callerUserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(connection);
    }

    public async Task<ConnectionDto> CancelRequestAsync(Guid callerUserId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        var connection = await _dbContext.Connections
            .FirstOrDefaultAsync(c => c.Id == connectionId, cancellationToken);

        if (connection == null)
            throw new DomainException("Connection request not found.");

        connection.Cancel(callerUserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(connection);
    }

    public async Task RemoveConnectionAsync(Guid callerUserId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        var (canonA, canonB) = callerUserId.CompareTo(targetUserId) < 0
            ? (callerUserId, targetUserId)
            : (targetUserId, callerUserId);

        var connection = await _dbContext.Connections
            .FirstOrDefaultAsync(c => c.CanonicalUserA == canonA && c.CanonicalUserB == canonB, cancellationToken);

        if (connection == null || connection.Status != ConnectionStatus.Accepted)
            throw new DomainException("No active connection found with this user.");

        _dbContext.Connections.Remove(connection);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConnectionDto>> GetIncomingRequestsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var requests = await _dbContext.Connections
            .AsNoTracking()
            .Where(c => c.AddresseeUserId == userId && c.Status == ConnectionStatus.Pending)
            .OrderByDescending(c => c.RequestedAtUtc)
            .ToListAsync(cancellationToken);

        var requesterIds = requests.Select(r => r.RequesterUserId).Distinct().ToList();
        var profiles = await _dbContext.UserProfiles
            .AsNoTracking()
            .Where(p => requesterIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, cancellationToken);
        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(u => requesterIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        return requests.Select(r =>
        {
            profiles.TryGetValue(r.RequesterUserId, out var p);
            users.TryGetValue(r.RequesterUserId, out var u);
            return new ConnectionDto(
                r.Id,
                r.RequesterUserId,
                r.AddresseeUserId,
                r.Status.ToString(),
                r.RequestedAtUtc,
                r.RespondedAtUtc,
                RequesterDisplayName: p?.DisplayName,
                RequesterAvatarUrl: p?.AvatarUrl,
                RequesterHeadline: p?.Headline,
                RequesterUsername: u?.UserName
            );
        }).ToList();
    }

    public async Task<IReadOnlyList<ConnectionDto>> GetOutgoingRequestsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var requests = await _dbContext.Connections
            .AsNoTracking()
            .Where(c => c.RequesterUserId == userId && c.Status == ConnectionStatus.Pending)
            .OrderByDescending(c => c.RequestedAtUtc)
            .ToListAsync(cancellationToken);

        var addresseeIds = requests.Select(r => r.AddresseeUserId).Distinct().ToList();
        var profiles = await _dbContext.UserProfiles
            .AsNoTracking()
            .Where(p => addresseeIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, cancellationToken);
        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(u => addresseeIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        return requests.Select(r =>
        {
            profiles.TryGetValue(r.AddresseeUserId, out var p);
            users.TryGetValue(r.AddresseeUserId, out var u);
            return new ConnectionDto(
                r.Id,
                r.RequesterUserId,
                r.AddresseeUserId,
                r.Status.ToString(),
                r.RequestedAtUtc,
                r.RespondedAtUtc,
                AddresseeDisplayName: p?.DisplayName,
                AddresseeAvatarUrl: p?.AvatarUrl,
                AddresseeHeadline: p?.Headline,
                AddresseeUsername: u?.UserName
            );
        }).ToList();
    }

    public async Task<IReadOnlyList<ConnectionUserDto>> GetAcceptedConnectionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var connections = await _dbContext.Connections
            .AsNoTracking()
            .Where(c => (c.CanonicalUserA == userId || c.CanonicalUserB == userId) && c.Status == ConnectionStatus.Accepted)
            .ToListAsync(cancellationToken);

        var otherUserIds = connections
            .Select(c => c.GetOtherUserId(userId))
            .Distinct()
            .ToList();

        var profiles = await _dbContext.UserProfiles
            .AsNoTracking()
            .Where(p => otherUserIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, cancellationToken);

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(u => otherUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var result = new List<ConnectionUserDto>();
        foreach (var conn in connections)
        {
            var otherId = conn.GetOtherUserId(userId);
            if (profiles.TryGetValue(otherId, out var profile))
            {
                users.TryGetValue(otherId, out var user);
                result.Add(new ConnectionUserDto(
                    otherId,
                    profile.DisplayName,
                    profile.Headline,
                    profile.AvatarUrl,
                    conn.RespondedAtUtc ?? conn.RequestedAtUtc,
                    user?.UserName
                ));
            }
        }

        return result;
    }

    public async Task<ConnectionStatusDto> GetConnectionStatusAsync(Guid callerUserId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        if (callerUserId == targetUserId)
            return new ConnectionStatusDto("Self", null, false);

        var isBlocked = await _dbContext.UserBlocks
            .AnyAsync(b => (b.BlockerUserId == callerUserId && b.BlockedUserId == targetUserId) ||
                           (b.BlockerUserId == targetUserId && b.BlockedUserId == callerUserId), cancellationToken);

        if (isBlocked)
            return new ConnectionStatusDto("Blocked", null, false);

        var (canonA, canonB) = callerUserId.CompareTo(targetUserId) < 0
            ? (callerUserId, targetUserId)
            : (targetUserId, callerUserId);

        var connection = await _dbContext.Connections
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CanonicalUserA == canonA && c.CanonicalUserB == canonB, cancellationToken);

        if (connection == null)
            return new ConnectionStatusDto("None", null, false);

        if (connection.Status == ConnectionStatus.Accepted)
            return new ConnectionStatusDto("Accepted", connection.Id, connection.RequesterUserId == callerUserId);

        if (connection.Status == ConnectionStatus.Pending)
        {
            return connection.RequesterUserId == callerUserId
                ? new ConnectionStatusDto("PendingOutgoing", connection.Id, true)
                : new ConnectionStatusDto("PendingIncoming", connection.Id, false);
        }

        return new ConnectionStatusDto(connection.Status.ToString(), connection.Id, connection.RequesterUserId == callerUserId);
    }

    private static ConnectionDto MapToDto(Connection connection) => new(
        connection.Id,
        connection.RequesterUserId,
        connection.AddresseeUserId,
        connection.Status.ToString(),
        connection.RequestedAtUtc,
        connection.RespondedAtUtc
    );
}
