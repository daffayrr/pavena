using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pavena.Application.Common.DTOs;
using Pavena.Application.Common.Interfaces;

namespace Pavena.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class StorageController : ControllerBase
{
    private readonly IObjectStorageService _storageService;
    private readonly IMalwareScanner _malwareScanner;
    private readonly ILogger<StorageController> _logger;

    public StorageController(
        IObjectStorageService storageService,
        IMalwareScanner malwareScanner,
        ILogger<StorageController> logger)
    {
        _storageService = storageService;
        _malwareScanner = malwareScanner;
        _logger = logger;
    }

    [Authorize]
    [HttpPost("upload-ticket")]
    public async Task<IActionResult> GenerateUploadTicket([FromBody] UploadTicketRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (!_storageService.ValidateFileMetadata(request.FileName, request.ContentType, request.SizeBytes, out var error))
        {
            return BadRequest(new { error });
        }

        var ticket = await _storageService.GenerateUploadTicketAsync(
            request.FileName,
            request.ContentType,
            request.SizeBytes,
            userId.Value,
            cancellationToken);

        return Ok(ticket);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(
        [FromQuery] string key,
        [FromQuery] long expires,
        [FromQuery] long size,
        [FromQuery] string sig,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key) || expires <= 0 || string.IsNullOrWhiteSpace(sig))
        {
            return BadRequest(new { error = "Missing storage ticket parameters." });
        }

        var isValid = _storageService.VerifyUploadSignature(key, expires, size, sig);
        if (!isValid)
        {
            _logger.LogWarning("Invalid or expired upload signature for key: {Key}", key);
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Upload ticket signature is invalid or expired." });
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file stream provided for upload." });
        }

        if (file.Length > size + 1024) // Small tolerance margin
        {
            return BadRequest(new { error = "Uploaded file exceeds the ticket's allocated byte size." });
        }

        await using var stream = file.OpenReadStream();

        // Malware & dangerous binary verification
        var scanResult = await _malwareScanner.ScanAsync(stream, file.FileName, cancellationToken);
        if (!scanResult.IsClean)
        {
            _logger.LogWarning("Malware scan rejected upload {FileName}: {Threat}", file.FileName, scanResult.ThreatName);
            return BadRequest(new
            {
                error = "File failed security scan.",
                threat = scanResult.ThreatName,
                details = scanResult.Message
            });
        }

        await _storageService.SaveObjectAsync(key, stream, file.ContentType, cancellationToken);

        _logger.LogInformation("Successfully stored object {Key} ({Size} bytes)", key, file.Length);
        return Ok(new
        {
            message = "File uploaded securely.",
            storageKey = key,
            sizeBytes = file.Length
        });
    }

    [HttpGet("download")]
    public async Task<IActionResult> DownloadFile(
        [FromQuery] string key,
        [FromQuery] long expires,
        [FromQuery] string sig,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key) || expires <= 0 || string.IsNullOrWhiteSpace(sig))
        {
            return BadRequest(new { error = "Missing signed token parameters." });
        }

        var isValid = _storageService.VerifyDownloadSignature(key, expires, sig);
        if (!isValid)
        {
            _logger.LogWarning("Invalid or expired download signature for key: {Key}", key);
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Download link is invalid or expired." });
        }

        try
        {
            var stream = await _storageService.GetObjectStreamAsync(key, cancellationToken);
            var fileName = Path.GetFileName(key);
            
            Response.Headers.Append("X-Content-Type-Options", "nosniff");
            return File(stream, "application/octet-stream", fileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = "The requested file does not exist." });
        }
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
