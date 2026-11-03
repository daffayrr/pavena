using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pavena.Application.Social.Interfaces;

namespace Pavena.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class FeedController : ControllerBase
{
    private readonly IFeedService _feedService;

    public FeedController(IFeedService feedService)
    {
        _feedService = feedService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyNetworkFeed(
        [FromQuery] int limit = 20,
        [FromQuery] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var feed = await _feedService.GetMyNetworkFeedAsync(callerUserId.Value, limit, cursor, cancellationToken);
        return Ok(feed);
    }

    [HttpGet("discover")]
    public async Task<IActionResult> GetDiscoverFeed(
        [FromQuery] int limit = 20,
        [FromQuery] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var feed = await _feedService.GetDiscoverFeedAsync(callerUserId.Value, limit, cursor, cancellationToken);
        return Ok(feed);
    }

    private Guid? GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idClaim, out var guid) ? guid : null;
    }
}
