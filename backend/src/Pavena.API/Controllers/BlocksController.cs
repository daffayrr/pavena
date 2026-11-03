using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pavena.Application.Social.DTOs;
using Pavena.Application.Social.Interfaces;

namespace Pavena.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/users")]
public class BlocksController : ControllerBase
{
    private readonly IBlockService _blockService;
    private readonly IValidator<BlockUserRequest> _validator;

    public BlocksController(
        IBlockService blockService,
        IValidator<BlockUserRequest> validator)
    {
        _blockService = blockService;
        _validator = validator;
    }

    [HttpPost("{userId:guid}/block")]
    public async Task<IActionResult> BlockUser(Guid userId, [FromBody] BlockUserRequest? request, CancellationToken cancellationToken)
    {
        var blockerId = GetCurrentUserId();
        if (!blockerId.HasValue)
            return Unauthorized();

        if (request != null)
        {
            var validation = await _validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return BadRequest(validation.ToDictionary());
        }

        await _blockService.BlockUserAsync(blockerId.Value, userId, request?.Reason, cancellationToken);
        return Ok(new { message = "User blocked successfully." });
    }

    [HttpDelete("{userId:guid}/block")]
    public async Task<IActionResult> UnblockUser(Guid userId, CancellationToken cancellationToken)
    {
        var blockerId = GetCurrentUserId();
        if (!blockerId.HasValue)
            return Unauthorized();

        await _blockService.UnblockUserAsync(blockerId.Value, userId, cancellationToken);
        return NoContent();
    }

    [HttpGet("me/blocks")]
    public async Task<IActionResult> GetMyBlocks(CancellationToken cancellationToken)
    {
        var blockerId = GetCurrentUserId();
        if (!blockerId.HasValue)
            return Unauthorized();

        var blockedUsers = await _blockService.GetBlockedUsersAsync(blockerId.Value, cancellationToken);
        return Ok(blockedUsers);
    }

    private Guid? GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idClaim, out var guid) ? guid : null;
    }
}
