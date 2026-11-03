using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pavena.Application.Common.Interfaces;
using Pavena.Application.Identity.DTOs;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Enums;

namespace Pavena.API.Controllers;

public record SuspendUserRequest(string ReasonCode, string? Details);

[ApiController]
[Authorize(Policy = "RequirePlatformAdmin")]
[Route("api/v1/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IPavenaDbContext _dbContext;
    private readonly UserManager<User> _userManager;

    public AdminController(IPavenaDbContext dbContext, UserManager<User> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users
            .AsNoTracking()
            .Include(u => u.Profile);

        var total = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = new List<UserSummaryDto>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            dtos.Add(new UserSummaryDto(
                u.Id,
                u.Email!,
                u.UserName!,
                u.Profile?.DisplayName ?? u.UserName!,
                u.Status.ToString(),
                u.EmailConfirmed,
                roles.ToList(),
                u.CreatedAtUtc
            ));
        }

        return Ok(new { total, page, pageSize, data = dtos });
    }

    [HttpPost("users/{id:guid}/suspend")]
    public async Task<IActionResult> SuspendUser(Guid id, [FromBody] SuspendUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound(new ProblemDetails { Detail = "User not found." });

        user.Status = UserStatus.Suspended;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _ = Guid.TryParse(adminIdStr, out var adminId);

        var auditLog = new AdminAuditLog
        {
            AdminUserId = adminId,
            ActionType = "USER_SUSPENSION",
            TargetResourceType = "USER",
            TargetResourceId = user.Id.ToString(),
            ReasonCode = request.ReasonCode ?? "POLICY_VIOLATION",
            DetailsJson = request.Details,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            TimestampUtc = DateTime.UtcNow
        };

        _dbContext.AdminAuditLogs.Add(auditLog);
        await _userManager.UpdateSecurityStampAsync(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { message = $"User '{user.UserName}' has been suspended.", status = user.Status.ToString() });
    }

    [HttpPost("users/{id:guid}/reactivate")]
    public async Task<IActionResult> ReactivateUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound(new ProblemDetails { Detail = "User not found." });

        user.Status = UserStatus.Active;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _ = Guid.TryParse(adminIdStr, out var adminId);

        var auditLog = new AdminAuditLog
        {
            AdminUserId = adminId,
            ActionType = "USER_REACTIVATION",
            TargetResourceType = "USER",
            TargetResourceId = user.Id.ToString(),
            ReasonCode = "ADMIN_OVERRIDE",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            TimestampUtc = DateTime.UtcNow
        };

        _dbContext.AdminAuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { message = $"User '{user.UserName}' has been reactivated.", status = user.Status.ToString() });
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AdminAuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.TimestampUtc);

        var total = await query.CountAsync(cancellationToken);
        var logs = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(new { total, page, pageSize, data = logs });
    }
}
