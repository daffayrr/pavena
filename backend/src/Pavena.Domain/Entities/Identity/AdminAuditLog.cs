using Pavena.Domain.Common;

namespace Pavena.Domain.Entities.Identity;

public class AdminAuditLog : BaseEntity
{
    public Guid AdminUserId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string TargetResourceType { get; set; } = string.Empty;
    public string TargetResourceId { get; set; } = string.Empty;
    public string ReasonCode { get; set; } = string.Empty;
    public string? DetailsJson { get; set; }
    public string? IpAddress { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public virtual User? AdminUser { get; set; }
}
