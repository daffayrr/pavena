using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pavena.Application.Social.DTOs;
using Pavena.Application.Social.Interfaces;

namespace Pavena.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class ConnectionsController : ControllerBase
{
    private readonly IConnectionService _connectionService;
    private readonly IValidator<SendConnectionRequest> _validator;

    public ConnectionsController(
        IConnectionService connectionService,
        IValidator<SendConnectionRequest> validator)
    {
        _connectionService = connectionService;
        _validator = validator;
    }

    [HttpPost("requests")]
    public async Task<IActionResult> SendRequest([FromBody] SendConnectionRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.ToDictionary());

        var result = await _connectionService.SendRequestAsync(userId.Value, request.AddresseeUserId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("requests/{id:guid}/accept")]
    public async Task<IActionResult> AcceptRequest(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _connectionService.AcceptRequestAsync(userId.Value, id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("requests/{id:guid}/reject")]
    public async Task<IActionResult> RejectRequest(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _connectionService.RejectRequestAsync(userId.Value, id, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("requests/{id:guid}")]
    public async Task<IActionResult> CancelRequest(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _connectionService.CancelRequestAsync(userId.Value, id, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> RemoveConnection(Guid userId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        await _connectionService.RemoveConnectionAsync(currentUserId.Value, userId, cancellationToken);
        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> GetAcceptedConnections(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var connections = await _connectionService.GetAcceptedConnectionsAsync(userId.Value, cancellationToken);
        return Ok(connections);
    }

    [HttpGet("requests/incoming")]
    public async Task<IActionResult> GetIncomingRequests(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var requests = await _connectionService.GetIncomingRequestsAsync(userId.Value, cancellationToken);
        return Ok(requests);
    }

    [HttpGet("requests/outgoing")]
    public async Task<IActionResult> GetOutgoingRequests(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var requests = await _connectionService.GetOutgoingRequestsAsync(userId.Value, cancellationToken);
        return Ok(requests);
    }

    [HttpGet("status/{userId:guid}")]
    public async Task<IActionResult> GetConnectionStatus(Guid userId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        var status = await _connectionService.GetConnectionStatusAsync(currentUserId.Value, userId, cancellationToken);
        return Ok(status);
    }

    private Guid? GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idClaim, out var guid) ? guid : null;
    }
}
