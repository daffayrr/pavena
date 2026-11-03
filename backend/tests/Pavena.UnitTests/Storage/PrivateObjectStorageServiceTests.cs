using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Pavena.Infrastructure.Storage;
using Xunit;

namespace Pavena.UnitTests.Storage;

public class PrivateObjectStorageServiceTests
{
    private readonly PrivateObjectStorageService _storageService;
    private readonly string _tempPath;

    public PrivateObjectStorageServiceTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), "pavena_storage_unit_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempPath);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:BasePath"] = _tempPath,
                ["Storage:SignedTokenSecret"] = "TestSecretKeyForHmacSignatures#2026!"
            })
            .Build();

        _storageService = new PrivateObjectStorageService(config, NullLogger<PrivateObjectStorageService>.Instance);
    }

    [Theory]
    [InlineData("methodology.pdf", "application/pdf", 1024 * 1024, true)]
    [InlineData("dataset.csv", "text/csv", 2048, true)]
    [InlineData("exploit.exe", "application/octet-stream", 500, false)]
    [InlineData("script.sh", "text/x-shellscript", 500, false)]
    [InlineData("../traversal.pdf", "application/pdf", 1024, false)]
    [InlineData("too_large.pdf", "application/pdf", 30 * 1024 * 1024, false)] // > 25MB
    public void ValidateFileMetadata_ValidatesSecurityRules(string fileName, string contentType, long sizeBytes, bool expectedValid)
    {
        var isValid = _storageService.ValidateFileMetadata(fileName, contentType, sizeBytes, out var error);
        isValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            error.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task GenerateUploadTicket_CreatesSignedTicket_AndVerifiesSuccessfully()
    {
        var userId = Guid.NewGuid();
        var ticket = await _storageService.GenerateUploadTicketAsync("thesis_draft.pdf", "application/pdf", 5000, userId);

        ticket.Should().NotBeNull();
        ticket.StorageKey.Should().Contain(userId.ToString("N"));
        ticket.StorageKey.Should().EndWith("thesis_draft.pdf");
        ticket.Signature.Should().NotBeNullOrEmpty();
        ticket.ExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);

        var expiresUnix = new DateTimeOffset(ticket.ExpiresAtUtc).ToUnixTimeSeconds();
        var isValid = _storageService.VerifyUploadSignature(ticket.StorageKey, expiresUnix, ticket.SizeBytes, ticket.Signature);
        isValid.Should().BeTrue();

        // Tampered signature must fail
        var isTamperedValid = _storageService.VerifyUploadSignature(ticket.StorageKey, expiresUnix, ticket.SizeBytes, ticket.Signature + "tampered");
        isTamperedValid.Should().BeFalse();

        // Expired signature must fail
        var isExpiredValid = _storageService.VerifyUploadSignature(ticket.StorageKey, expiresUnix - 3600, ticket.SizeBytes, ticket.Signature);
        isExpiredValid.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateDownloadUrl_GeneratesVerifiableSignedUrl()
    {
        var storageKey = "storage/posts/user123/file.pdf";
        var downloadUrl = await _storageService.GenerateDownloadUrlAsync(storageKey, TimeSpan.FromMinutes(15));

        downloadUrl.Should().Contain("key=");
        downloadUrl.Should().Contain("sig=");
        downloadUrl.Should().Contain("expires=");

        // Extract query parameters
        var uri = new Uri("http://localhost" + downloadUrl);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var key = query["key"]!;
        var expires = long.Parse(query["expires"]!);
        var sig = query["sig"]!;

        var isValid = _storageService.VerifyDownloadSignature(key, expires, sig);
        isValid.Should().BeTrue();

        // Tampering storage key must invalidate signature
        var isTamperedValid = _storageService.VerifyDownloadSignature("storage/posts/otheruser/file.pdf", expires, sig);
        isTamperedValid.Should().BeFalse();
    }

    [Fact]
    public async Task Save_Get_Delete_Operations_WorkWithSecureLocalIsolation()
    {
        var storageKey = "storage/posts/author1/paper.txt";
        var contentString = "Academic Manuscript Draft 2026";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(contentString));

        // 1. Save
        await _storageService.SaveObjectAsync(storageKey, stream, "text/plain");

        // 2. Read
        using (var readStream = await _storageService.GetObjectStreamAsync(storageKey))
        using (var reader = new StreamReader(readStream))
        {
            var readContent = await reader.ReadToEndAsync();
            readContent.Should().Be(contentString);
        }

        // 3. Delete
        await _storageService.DeleteObjectAsync(storageKey);

        // 4. Verify gone
        var act = async () => await _storageService.GetObjectStreamAsync(storageKey);
        await act.Should().ThrowAsync<FileNotFoundException>();
    }
}
