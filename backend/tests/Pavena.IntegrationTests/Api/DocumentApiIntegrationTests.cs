using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Pavena.Application.Documents.DTOs;
using Pavena.Application.Identity.DTOs;
using Pavena.Domain.Enums;
using Xunit;

namespace Pavena.IntegrationTests.Api;

public class DocumentApiIntegrationTests : IClassFixture<PavenaApiFactory>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public DocumentApiIntegrationTests(PavenaApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid UserId, string Email)> CreateAndAuthenticateUserAsync(string prefix)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"{prefix}_{suffix}@academic.test";
        var username = $"{prefix}_{suffix}";
        var password = "StrongPass#2026";

        var registerReq = new RegisterRequest(email, username, password, $"Dr. {prefix}", "GB");
        var regRes = await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        regRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginReq = new LoginRequest(email, password);
        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        loginRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var authBody = await loginRes.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions);
        return (authBody!.AccessToken, authBody.UserId, email);
    }

    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url, string token, object? body = null)
    {
        var msg = new HttpRequestMessage(method, url);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body != null)
        {
            msg.Content = JsonContent.Create(body);
        }
        return msg;
    }

    [Fact]
    public async Task DocumentLifecycle_Create_Files_Compile_Versioning_Restore_WorksEndToEnd()
    {
        var (token, userId, _) = await CreateAndAuthenticateUserAsync("latexAuthor");

        // 1. Create Document
        var createDocReq = new CreateDocumentRequest(
            Title: "Quantum Graph Neural Networks",
            Description: "Hybrid quantum-classical geometric architectures",
            DocumentType: "Manuscript",
            Mode: EditorMode.Latex,
            ProjectId: null,
            Visibility: DocumentVisibility.Private
        );

        using var createMsg = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/documents", token, createDocReq);
        var createRes = await _client.SendAsync(createMsg);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var docSummary = await createRes.Content.ReadFromJsonAsync<DocumentSummaryDto>(_jsonOptions);
        docSummary.Should().NotBeNull();
        var docId = docSummary!.Id;

        // 2. Fetch Document Detail
        using var getMsg = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/documents/{docId}", token);
        var getRes = await _client.SendAsync(getMsg);
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var docDetail = await getRes.Content.ReadFromJsonAsync<DocumentDetailDto>(_jsonOptions);
        docDetail.Should().NotBeNull();
        docDetail!.Files.Should().HaveCount(1);
        var mainFile = docDetail.Files.First();
        mainFile.FileName.Should().Be("main.tex");
        mainFile.IsMain.Should().BeTrue();

        // 3. Create Additional File (references.bib)
        var createFileReq = new CreateFileRequest("references.bib", "@article{einstein1905,\n  author = {Albert Einstein}\n}");
        using var addFileMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/documents/{docId}/files", token, createFileReq);
        var addFileRes = await _client.SendAsync(addFileMsg);
        addFileRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var bibFile = await addFileRes.Content.ReadFromJsonAsync<DocumentFileDto>(_jsonOptions);
        bibFile.Should().NotBeNull();
        bibFile!.FileName.Should().Be("references.bib");

        // 4. Update main.tex content
        var updatedTex = @"\documentclass{article}
\title{Quantum Graph Neural Networks}
\author{Dr. Author}
\begin{document}
\maketitle
\section{Introduction}
We formulate equivariant quantum circuits over topological manifolds.
\end{document}";
        using var updateContentMsg = CreateAuthenticatedRequest(HttpMethod.Put, $"/api/v1/documents/{docId}/files/{mainFile.Id}/content", token, new UpdateFileContentRequest(updatedTex));
        var updateContentRes = await _client.SendAsync(updateContentMsg);
        updateContentRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify read back
        using var readContentMsg = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/documents/{docId}/files/{mainFile.Id}/content", token);
        var readContentRes = await _client.SendAsync(readContentMsg);
        readContentRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var contentJson = await readContentRes.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        contentJson.GetProperty("content").GetString().Should().Contain("equivariant quantum circuits");

        // 5. Trigger LaTeX Compilation
        using var compileMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/documents/{docId}/compile", token, new TriggerCompilationRequest(CompilationEngine.PdfLatex));
        var compileRes = await _client.SendAsync(compileMsg);
        compileRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var compileJob = await compileRes.Content.ReadFromJsonAsync<CompilationJobDto>(_jsonOptions);
        compileJob.Should().NotBeNull();
        compileJob!.Status.Should().Be(CompilationStatus.Succeeded);
        compileJob.OutputLog.Should().NotBeNullOrEmpty();

        // 6. Download Compiled PDF
        using var downloadPdfMsg = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/documents/{docId}/compile/{compileJob.Id}/pdf", token);
        var downloadPdfRes = await _client.SendAsync(downloadPdfMsg);
        downloadPdfRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var pdfBytes = await downloadPdfRes.Content.ReadAsByteArrayAsync();
        pdfBytes.Should().NotBeEmpty();
        System.Text.Encoding.ASCII.GetString(pdfBytes[..5]).Should().Be("%PDF-");

        // 7. Create Snapshot Version
        var versionReq = new CreateVersionRequest("Camera Ready v1", "First stable draft with bibliography");
        using var createVersionMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/documents/{docId}/versions", token, versionReq);
        var createVersionRes = await _client.SendAsync(createVersionMsg);
        createVersionRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var versionDto = await createVersionRes.Content.ReadFromJsonAsync<DocumentVersionDto>(_jsonOptions);
        versionDto.Should().NotBeNull();
        versionDto!.VersionNumber.Should().Be(1);
        versionDto.Title.Should().Be("Camera Ready v1");

        // 8. Restore Snapshot
        using var restoreMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/documents/{docId}/versions/{versionDto.Id}/restore", token);
        var restoreRes = await _client.SendAsync(restoreMsg);
        restoreRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var restoredDetail = await restoreRes.Content.ReadFromJsonAsync<DocumentDetailDto>(_jsonOptions);
        restoredDetail.Should().NotBeNull();
        restoredDetail!.Files.Should().HaveCount(2);
    }

    [Fact]
    public async Task DocumentSecurity_UnauthorizedUser_Receives404ConcealingDocument_ThenCanAccessOnceGranted()
    {
        var (tokenA, userAId, _) = await CreateAndAuthenticateUserAsync("ownerUser");
        var (tokenB, userBId, _) = await CreateAndAuthenticateUserAsync("unauthorizedUser");

        // User A creates private document
        var createDocReq = new CreateDocumentRequest(
            Title: "Proprietary Algorithm Specification",
            Description: "Classified commercial IP",
            DocumentType: "Proposal",
            Mode: EditorMode.Latex,
            ProjectId: null,
            Visibility: DocumentVisibility.Private
        );
        using var createMsg = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/documents", tokenA, createDocReq);
        var createRes = await _client.SendAsync(createMsg);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var doc = (await createRes.Content.ReadFromJsonAsync<DocumentSummaryDto>(_jsonOptions))!;

        // User B tries to view -> Must be 404 NotFound to conceal existence
        using var unauthGetMsg = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/documents/{doc.Id}", tokenB);
        var unauthGetRes = await _client.SendAsync(unauthGetMsg);
        unauthGetRes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // User A adds User B as Editor
        var addCollabReq = new AddCollaboratorRequest(userBId, DocumentPermissionRole.Editor);
        using var addCollabMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/documents/{doc.Id}/collaborators", tokenA, addCollabReq);
        var addCollabRes = await _client.SendAsync(addCollabMsg);
        addCollabRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // User B tries to view again -> Succeeded (200 OK)
        using var authGetMsg = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/documents/{doc.Id}", tokenB);
        var authGetRes = await _client.SendAsync(authGetMsg);
        authGetRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await authGetRes.Content.ReadFromJsonAsync<DocumentDetailDto>(_jsonOptions);
        detail.Should().NotBeNull();
        detail!.UserRole.Should().Be("Editor");
    }

    [Fact]
    public async Task DocumentCommentsAndReviews_Workflow_FunctionsAsExpected()
    {
        var (tokenA, userAId, _) = await CreateAndAuthenticateUserAsync("authorLead");
        var (tokenB, userBId, _) = await CreateAndAuthenticateUserAsync("peerReviewer");

        // User A creates document
        var createDocReq = new CreateDocumentRequest("Topological Data Analysis", null, "Manuscript", EditorMode.Latex, null);
        using var createMsg = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/documents", tokenA, createDocReq);
        var createRes = await _client.SendAsync(createMsg);
        var doc = (await createRes.Content.ReadFromJsonAsync<DocumentSummaryDto>(_jsonOptions))!;

        // User A adds User B as Reviewer
        using var addCollabMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/documents/{doc.Id}/collaborators", tokenA, new AddCollaboratorRequest(userBId, DocumentPermissionRole.Reviewer));
        await _client.SendAsync(addCollabMsg);

        // User B adds comment on document
        var commentReq = new CreateCommentRequest("Definition 2.1 needs boundary condition check.", null, 15, "persistent homology");
        using var addCommentMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/documents/{doc.Id}/comments", tokenB, commentReq);
        var addCommentRes = await _client.SendAsync(addCommentMsg);
        addCommentRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var commentDto = await addCommentRes.Content.ReadFromJsonAsync<DocumentCommentDto>(_jsonOptions);
        commentDto.Should().NotBeNull();
        commentDto!.Content.Should().Be("Definition 2.1 needs boundary condition check.");
        commentDto.Status.Should().Be(CommentStatus.Active);

        // User A requests formal review from User B
        using var reqReviewMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/documents/{doc.Id}/reviews", tokenA, new RequestReviewRequest(userBId));
        var reqReviewRes = await _client.SendAsync(reqReviewMsg);
        reqReviewRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviewDto = await reqReviewRes.Content.ReadFromJsonAsync<DocumentReviewDto>(_jsonOptions);

        // User B submits approval decision
        using var decisionMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/documents/{doc.Id}/reviews/{reviewDto!.Id}/decision", tokenB,
            new SubmitReviewDecisionRequest(ReviewStatus.Approved, "Mathematical rigor is demonstrated. Ready for release."));
        var decisionRes = await _client.SendAsync(decisionMsg);
        decisionRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var finalReview = await decisionRes.Content.ReadFromJsonAsync<DocumentReviewDto>(_jsonOptions);
        finalReview!.Status.Should().Be(ReviewStatus.Approved);
        finalReview.SummaryNotes.Should().Contain("Ready for release");
    }
}
