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
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly IValidator<CreatePostRequest> _createValidator;
    private readonly IValidator<UpdatePostRequest> _updateValidator;

    public PostsController(
        IPostService postService,
        IValidator<CreatePostRequest> createValidator,
        IValidator<UpdatePostRequest> updateValidator)
    {
        _postService = postService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(validation.ToDictionary());

        var post = await _postService.CreatePostAsync(callerUserId.Value, request, cancellationToken);
        return CreatedAtAction(nameof(GetPostById), new { postId = post.Id }, post);
    }

    [HttpGet("{postId:guid}")]
    public async Task<IActionResult> GetPostById(Guid postId, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var post = await _postService.GetPostByIdAsync(callerUserId.Value, postId, cancellationToken);
        return Ok(post);
    }

    [HttpPut("{postId:guid}")]
    public async Task<IActionResult> UpdatePost(Guid postId, [FromBody] UpdatePostRequest request, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(validation.ToDictionary());

        var post = await _postService.UpdatePostAsync(callerUserId.Value, postId, request, cancellationToken);
        return Ok(post);
    }

    [HttpDelete("{postId:guid}")]
    public async Task<IActionResult> DeletePost(Guid postId, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        await _postService.DeletePostAsync(callerUserId.Value, postId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{postId:guid}/publish")]
    public async Task<IActionResult> PublishPost(Guid postId, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var post = await _postService.PublishPostAsync(callerUserId.Value, postId, cancellationToken);
        return Ok(post);
    }

    [HttpPost("{postId:guid}/archive")]
    public async Task<IActionResult> ArchivePost(Guid postId, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var post = await _postService.ArchivePostAsync(callerUserId.Value, postId, cancellationToken);
        return Ok(post);
    }

    private Guid? GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idClaim, out var guid) ? guid : null;
    }
}
