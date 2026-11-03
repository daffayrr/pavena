using Pavena.Domain.Common;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Enums;

namespace Pavena.Domain.Entities.Social;

public class Connection : BaseEntity
{
    public Guid RequesterUserId { get; set; }
    public Guid AddresseeUserId { get; set; }
    public Guid CanonicalUserA { get; set; }
    public Guid CanonicalUserB { get; set; }
    public ConnectionStatus Status { get; set; } = ConnectionStatus.Pending;
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAtUtc { get; set; }

    public virtual User? RequesterUser { get; set; }
    public virtual User? AddresseeUser { get; set; }

    public Connection()
    {
    }

    public static Connection CreateRequest(Guid requesterId, Guid addresseeId)
    {
        if (requesterId == Guid.Empty || addresseeId == Guid.Empty)
            throw new DomainException("User identifiers cannot be empty.");

        if (requesterId == addresseeId)
            throw new DomainException("A user cannot send a connection request to themselves.");

        var (canonA, canonB) = requesterId.CompareTo(addresseeId) < 0
            ? (requesterId, addresseeId)
            : (addresseeId, requesterId);

        return new Connection
        {
            RequesterUserId = requesterId,
            AddresseeUserId = addresseeId,
            CanonicalUserA = canonA,
            CanonicalUserB = canonB,
            Status = ConnectionStatus.Pending,
            RequestedAtUtc = DateTime.UtcNow,
            RespondedAtUtc = null
        };
    }

    public void ReRequest(Guid newRequesterId, Guid newAddresseeId)
    {
        if (newRequesterId == Guid.Empty || newAddresseeId == Guid.Empty)
            throw new DomainException("User identifiers cannot be empty.");

        if (newRequesterId == newAddresseeId)
            throw new DomainException("A user cannot send a connection request to themselves.");

        if (Status == ConnectionStatus.Pending)
            throw new DomainException("A connection request is already pending between these users.");

        if (Status == ConnectionStatus.Accepted)
            throw new DomainException("Users are already connected.");

        RequesterUserId = newRequesterId;
        AddresseeUserId = newAddresseeId;
        Status = ConnectionStatus.Pending;
        RequestedAtUtc = DateTime.UtcNow;
        RespondedAtUtc = null;
    }

    public void Accept(Guid callerUserId)
    {
        if (callerUserId != AddresseeUserId)
            throw new DomainException("Only the recipient of a connection request can accept it.");

        if (Status != ConnectionStatus.Pending)
            throw new DomainException($"Cannot accept a connection in '{Status}' status.");

        Status = ConnectionStatus.Accepted;
        RespondedAtUtc = DateTime.UtcNow;
    }

    public void Reject(Guid callerUserId)
    {
        if (callerUserId != AddresseeUserId)
            throw new DomainException("Only the recipient of a connection request can reject it.");

        if (Status != ConnectionStatus.Pending)
            throw new DomainException($"Cannot reject a connection in '{Status}' status.");

        Status = ConnectionStatus.Rejected;
        RespondedAtUtc = DateTime.UtcNow;
    }

    public void Cancel(Guid callerUserId)
    {
        if (callerUserId != RequesterUserId)
            throw new DomainException("Only the sender of a connection request can cancel it.");

        if (Status != ConnectionStatus.Pending)
            throw new DomainException($"Cannot cancel a connection in '{Status}' status.");

        Status = ConnectionStatus.Cancelled;
        RespondedAtUtc = DateTime.UtcNow;
    }

    public bool IsParticipant(Guid userId)
    {
        return RequesterUserId == userId || AddresseeUserId == userId;
    }

    public Guid GetOtherUserId(Guid userId)
    {
        if (userId == RequesterUserId) return AddresseeUserId;
        if (userId == AddresseeUserId) return RequesterUserId;
        throw new DomainException("User is not a participant in this connection.");
    }
}
