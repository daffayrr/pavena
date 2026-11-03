using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pavena.Application.Social.Interfaces;

namespace Pavena.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/users")]
public class FollowsController : ControllerBase
{
    private readonly IFollowService _followService;

    public FollowsController(IFollowService followService)
    {
        _followService = followService;
    }

    [HttpPost("{userId:guid}/follow")]
    public async Task<IActionResult> Follow(Guid userId, CancellationToken cancellationToken)
    {
        var followerId = GetCurrentUserId();
        if (!followerId.HasValue)
            return Unauthorized();

        await _followService.FollowUserAsync(followerId.Value, userId, cancellationToken);
        return Ok(new { message = "Successfully followed user." });
    }

    [HttpDelete("{userId:guid}/follow")]
    public async Task<IActionResult> Unfollow(Guid userId, CancellationToken cancellationToken)
    {
        var followerId = GetCurrentUserId();
        if (!followerId.HasValue)
            return Unauthorized();

        await _followService.UnfollowUserAsync(followerId.Value, userId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{userId:guid}/followers")]
    public async Task<IActionResult> GetFollowers(Guid userId, CancellationToken cancellationToken)
    {
        var followers = await _followService.GetFollowersAsync(userId, cancellationToken);
        return Ok(followers);
    }

    [HttpGet("{userId:guid}/following")]
    public async Task<IActionResult> GetFollowing(Guid userId, CancellationToken cancellationToken)
    {
        var following = await _followService.GetFollowingAsync(userId, cancellationToken);
        return Ok(following);
    }

    [HttpGet("{userId:guid}/follow-status")]
    public async Task<IActionResult> GetFollowStatus(Guid userId, CancellationToken cancellationToken)
    {
        var callerId = GetCurrentUserId();
        if (!callerId.HasValue)
            return Unauthorized();

        var status = await _followService.GetFollowStatusAsync(callerId.Value, userId, cancellationToken);
        return Ok(status);
    }

    private Guid? GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idClaim, out var guid) ? guid : null;
    }
}
