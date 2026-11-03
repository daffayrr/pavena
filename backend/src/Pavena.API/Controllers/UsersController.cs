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
    private readonly IValidator<CreateEducationRequest>? _educationValidator;
    private readonly IValidator<CreateExperienceRequest>? _experienceValidator;
    private readonly IValidator<CreateSkillRequest>? _skillValidator;

    public UsersController(
        IUserProfileService profileService,
        IPostService postService,
        IValidator<UpdateProfileRequest> updateValidator,
        IValidator<CreateEducationRequest>? educationValidator = null,
        IValidator<CreateExperienceRequest>? experienceValidator = null,
        IValidator<CreateSkillRequest>? skillValidator = null)
    {
        _profileService = profileService;
        _postService = postService;
        _updateValidator = updateValidator;
        _educationValidator = educationValidator;
        _experienceValidator = experienceValidator;
        _skillValidator = skillValidator;
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

    [HttpGet("discover")]
    public async Task<IActionResult> DiscoverResearchers(
        [FromQuery] string? q,
        [FromQuery] string? institution,
        [FromQuery] string? expertise,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var callerId = GetCurrentUserId();
        var researchers = await _profileService.DiscoverResearchersAsync(callerId, q, institution, expertise, limit, offset, cancellationToken);
        return Ok(researchers);
    }

    [HttpGet("{identifier}/academic-profile")]
    public async Task<IActionResult> GetAcademicProfile(string identifier, CancellationToken cancellationToken)
    {
        var callerId = GetCurrentUserId();
        var profile = await _profileService.GetAcademicProfileAsync(identifier, callerId, cancellationToken);
        if (profile == null)
            return NotFound(new ProblemDetails { Detail = "Academic researcher profile not found." });

        return Ok(profile);
    }

    [Authorize]
    [HttpGet("me/academic-profile")]
    public async Task<IActionResult> GetMyAcademicProfile(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var profile = await _profileService.GetAcademicProfileByUserIdAsync(userId.Value, userId.Value, cancellationToken);
        if (profile == null)
            return NotFound(new ProblemDetails { Detail = "Academic profile not found." });

        return Ok(profile);
    }

    [Authorize]
    [HttpPost("me/education")]
    public async Task<IActionResult> AddEducation([FromBody] CreateEducationRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (_educationValidator != null)
        {
            var validation = await _educationValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var result = await _profileService.AddEducationAsync(userId.Value, request, cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpDelete("me/education/{id:guid}")]
    public async Task<IActionResult> DeleteEducation(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var success = await _profileService.DeleteEducationAsync(userId.Value, id, cancellationToken);
        if (!success)
            return NotFound(new ProblemDetails { Detail = "Education entry not found or unauthorized." });

        return NoContent();
    }

    [Authorize]
    [HttpPost("me/experience")]
    public async Task<IActionResult> AddExperience([FromBody] CreateExperienceRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (_experienceValidator != null)
        {
            var validation = await _experienceValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var result = await _profileService.AddExperienceAsync(userId.Value, request, cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpDelete("me/experience/{id:guid}")]
    public async Task<IActionResult> DeleteExperience(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var success = await _profileService.DeleteExperienceAsync(userId.Value, id, cancellationToken);
        if (!success)
            return NotFound(new ProblemDetails { Detail = "Experience entry not found or unauthorized." });

        return NoContent();
    }

    [Authorize]
    [HttpPost("me/skills")]
    public async Task<IActionResult> AddSkill([FromBody] CreateSkillRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (_skillValidator != null)
        {
            var validation = await _skillValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var result = await _profileService.AddSkillAsync(userId.Value, request.SkillName, cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpDelete("me/skills/{id:guid}")]
    public async Task<IActionResult> DeleteSkill(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var success = await _profileService.DeleteSkillAsync(userId.Value, id, cancellationToken);
        if (!success)
            return NotFound(new ProblemDetails { Detail = "Skill not found or unauthorized." });

        return NoContent();
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
