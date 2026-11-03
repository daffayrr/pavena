using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pavena.Application.Common.Interfaces;
using Pavena.Application.Social.DTOs;
using Pavena.Application.Social.Interfaces;

namespace Pavena.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly IObjectStorageService _storageService;
    private readonly IValidator<CreatePostRequest> _createValidator;
    private readonly IValidator<UpdatePostRequest> _updateValidator;

    public PostsController(
        IPostService postService,
        IObjectStorageService storageService,
        IValidator<CreatePostRequest> createValidator,
        IValidator<UpdatePostRequest> updateValidator)
    {
        _postService = postService;
        _storageService = storageService;
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

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyPosts(CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var posts = await _postService.GetUserPostsAsync(callerUserId.Value, callerUserId.Value, cancellationToken);
        return Ok(posts);
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

    [HttpGet("{postId:guid}/attachments/{attachmentId:guid}/download")]
    public async Task<IActionResult> GetAttachmentDownloadUrl(Guid postId, Guid attachmentId, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        var (storageKey, fileName, contentType, sizeBytes) = await _postService.GetAttachmentDownloadInfoAsync(
            callerUserId.Value, postId, attachmentId, cancellationToken);

        var signedUrl = await _storageService.GenerateDownloadUrlAsync(storageKey, TimeSpan.FromMinutes(15), cancellationToken);

        return Ok(new
        {
            downloadUrl = signedUrl,
            fileName,
            contentType,
            sizeBytes,
            expiresInMinutes = 15
        });
    }

    [HttpDelete("{postId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment(Guid postId, Guid attachmentId, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        if (!callerUserId.HasValue)
            return Unauthorized();

        await _postService.DeleteAttachmentAsync(callerUserId.Value, postId, attachmentId, cancellationToken);
        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idClaim, out var guid) ? guid : null;
    }
}
