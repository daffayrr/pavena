using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pavena.Application.Identity.DTOs;
using Pavena.Application.Identity.Interfaces;
using Pavena.Application.Social.Interfaces;

namespace Pavena.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserProfileService _profileService;
    private readonly IPostService _postService;
    private readonly IValidator<UpdateProfileRequest> _updateValidator;

    public UsersController(
        IUserProfileService profileService,
        IPostService postService,
        IValidator<UpdateProfileRequest> updateValidator)
    {
        _profileService = profileService;
        _postService = postService;
        _updateValidator = updateValidator;
    }

    [HttpGet("{username}")]
    public async Task<IActionResult> GetByUsername(string username, CancellationToken cancellationToken)
    {
        var callerId = GetCurrentUserId();
        var profile = await _profileService.GetProfileByUsernameAsync(username, callerId, cancellationToken);
        if (profile == null)
            return NotFound(new ProblemDetails { Detail = "User profile not found." });

        return Ok(profile);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var profile = await _profileService.GetProfileByUserIdAsync(userId.Value, userId.Value, cancellationToken);
        if (profile == null)
            return NotFound(new ProblemDetails { Detail = "Profile not found." });

        return Ok(profile);
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));

        var updated = await _profileService.UpdateProfileAsync(userId.Value, request, cancellationToken);
        return Ok(updated);
    }

    [Authorize]
    [HttpGet("{userId:guid}/posts")]
    public async Task<IActionResult> GetUserPosts(Guid userId, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var posts = await _postService.GetUserPostsAsync(callerUserId.Value, userId, cancellationToken);
        return Ok(posts);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var guid) ? guid : null;
    }
}
