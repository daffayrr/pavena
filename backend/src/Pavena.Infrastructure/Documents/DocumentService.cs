using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pavena.Application.Common.Authorization;
using Pavena.Application.Common.Interfaces;
using Pavena.Application.Documents.DTOs;
using Pavena.Application.Documents.Interfaces;
using Pavena.Domain.Common;
using Pavena.Domain.Entities.Documents;
using Pavena.Domain.Enums;

namespace Pavena.Infrastructure.Documents;

public class DocumentService : IDocumentService
{
    private readonly IPavenaDbContext _dbContext;
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IObjectStorageService _storageService;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IPavenaDbContext dbContext,
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor,
        IObjectStorageService storageService,
        ILogger<DocumentService> logger)
    {
        _dbContext = dbContext;
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
        _storageService = storageService;
        _logger = logger;
    }

    private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User ??
        new ClaimsPrincipal(new ClaimsIdentity());

    public async Task<DocumentSummaryDto> CreateDocumentAsync(
        Guid currentUserId,
        CreateDocumentRequest request,
        CancellationToken ct = default)
    {
        if (request.ProjectId.HasValue)
        {
            var isProjectMember = await _dbContext.ResearchMembers
                .AsNoTracking()
                .AnyAsync(m => m.ProjectId == request.ProjectId.Value &&
                               m.UserId == currentUserId &&
                               m.Status == ProjectMembershipStatus.Active, ct);

            var isProjectOwner = await _dbContext.ResearchProjects
                .AsNoTracking()
                .AnyAsync(p => p.Id == request.ProjectId.Value && p.OwnerUserId == currentUserId, ct);

            if (!isProjectMember && !isProjectOwner)
            {
                throw new UnauthorizedAccessException("You must be an active project member to associate documents with this research project.");
            }
        }

        var document = Document.Create(
            request.Title,
            request.Mode,
            currentUserId,
            request.ProjectId,
            request.Description,
            request.DocumentType
        );
        document.Visibility = request.Visibility;

        _dbContext.Documents.Add(document);

        // Add owner collaborator
        var ownerCollaborator = new DocumentCollaborator(document.Id, currentUserId, DocumentPermissionRole.Owner, currentUserId);
        _dbContext.DocumentCollaborators.Add(ownerCollaborator);

        // Save document and owner collaborator first to prevent circular dependency with mainFile
        await _dbContext.SaveChangesAsync(ct);

        // Create default initial file
        var isLatex = request.Mode == EditorMode.Latex;
        var fileName = isLatex ? "main.tex" : "document.json";
        var mimeType = isLatex ? "application/x-tex" : "application/json";
        var initialContent = isLatex
            ? GenerateDefaultLatexTemplate(request.Title)
            : "{\n  \"type\": \"doc\",\n  \"content\": []\n}";

        var initialBytes = Encoding.UTF8.GetBytes(initialContent);
        var fileId = Guid.NewGuid();
        var storageKey = $"documents/{document.Id}/files/{fileId}/{fileName}";

        using (var memStream = new MemoryStream(initialBytes))
        {
            await _storageService.SaveObjectAsync(storageKey, memStream, mimeType, ct);
        }

        var mainFile = DocumentFile.Create(
            document.Id,
            fileName,
            fileName,
            Path.GetExtension(fileName).ToLowerInvariant(),
            mimeType,
            initialBytes.Length,
            storageKey,
            true,
            false,
            currentUserId
        );

        _dbContext.DocumentFiles.Add(mainFile);
        document.MainFileId = mainFile.Id;

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Document {DocumentId} '{Title}' created by user {UserId}.", document.Id, document.Title, currentUserId);

        var ownerUser = await _dbContext.Users.AsNoTracking().Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == currentUserId, ct);
        var ownerName = ownerUser?.Profile?.DisplayName ?? ownerUser?.UserName ?? "Author";

        string? projectTitle = null;
        if (document.ProjectId.HasValue)
        {
            projectTitle = await _dbContext.ResearchProjects
                .Where(p => p.Id == document.ProjectId.Value)
                .Select(p => p.Title)
                .FirstOrDefaultAsync(ct);
        }

        return new DocumentSummaryDto(
            document.Id,
            document.Title,
            document.Description,
            document.DocumentType,
            document.Mode,
            document.ProjectId,
            projectTitle,
            document.OwnerUserId,
            ownerName,
            document.Visibility,
            document.Status,
            document.MainFileId,
            fileName,
            document.LastCompiledAtUtc,
            document.LastCompilationStatus,
            document.UpdatedAtUtc,
            document.CreatedAtUtc,
            1,
            1
        );
    }

    public async Task<DocumentDetailDto?> GetDocumentByIdAsync(
        Guid currentUserId,
        Guid documentId,
        CancellationToken ct = default)
    {
        var document = await _dbContext.Documents
            .Include(d => d.OwnerUser)
                .ThenInclude(u => u!.Profile)
            .Include(d => d.Project)
            .Include(d => d.MainFile)
            .Include(d => d.Files)
            .Include(d => d.Collaborators)
                .ThenInclude(c => c.User)
                    .ThenInclude(u => u!.Profile)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.Status != DocumentStatus.Trashed, ct);

        if (document == null)
            return null;

        var authResult = await _authorizationService.AuthorizeAsync(User, document, new ResourceOperationRequirement(ResourceOperation.Read));
        if (!authResult.Succeeded)
        {
            // Fallback direct check if currentUserId was passed explicitly
            var hasAccess = document.OwnerUserId == currentUserId ||
                            document.Visibility == DocumentVisibility.Public ||
                            document.Collaborators.Any(c => c.UserId == currentUserId && c.IsActive());

            if (!hasAccess)
                return null; // Return null to conceal private document existence (404)
        }

        var files = document.Files
            .OrderBy(f => f.Path)
            .Select(f => new DocumentFileDto(
                f.Id,
                f.DocumentId,
                f.Path,
                f.FileName,
                f.FileExtension,
                f.MimeType,
                f.SizeBytes,
                f.StorageKey,
                f.IsMain,
                f.IsBinary,
                f.UpdatedAtUtc,
                f.CreatedAtUtc
            )).ToList();

        var collaborators = document.Collaborators
            .Select(c => new CollaboratorDto(
                c.Id,
                c.DocumentId,
                c.UserId,
                c.User?.Profile?.DisplayName ?? c.User?.UserName ?? "Collaborator",
                c.User?.Email ?? string.Empty,
                c.Role,
                c.GrantedAtUtc,
                c.ExpiresAtUtc,
                c.IsActive(),
                c.CanCompile,
                c.CanReview
            )).ToList();

        var userRole = "Viewer";
        if (document.OwnerUserId == currentUserId)
        {
            userRole = "Owner";
        }
        else
        {
            var myCollab = document.Collaborators.FirstOrDefault(c => c.UserId == currentUserId && c.IsActive());
            if (myCollab != null)
                userRole = myCollab.Role.ToString();
        }

        var ownerName = document.OwnerUser?.Profile?.DisplayName ?? document.OwnerUser?.UserName ?? "Author";

        return new DocumentDetailDto(
            document.Id,
            document.Title,
            document.Description,
            document.DocumentType,
            document.Mode,
            document.ProjectId,
            document.Project?.Title,
            document.OwnerUserId,
            ownerName,
            document.Visibility,
            document.Status,
            document.MainFileId,
            document.MainFile?.FileName,
            document.IsRealTimeEnabled,
            document.LastCompiledAtUtc,
            document.LastCompilationStatus,
            document.ConcurrencyStamp,
            document.UpdatedAtUtc,
            document.CreatedAtUtc,
            files,
            collaborators,
            userRole
        );
    }

    public async Task<List<DocumentSummaryDto>> GetUserDocumentsAsync(
        Guid currentUserId,
        Guid? projectId = null,
        CancellationToken ct = default)
    {
        var accessibleDocIds = await _dbContext.DocumentCollaborators
            .AsNoTracking()
            .Where(c => c.UserId == currentUserId && c.RevokedAtUtc == null && (!c.ExpiresAtUtc.HasValue || c.ExpiresAtUtc > DateTime.UtcNow))
            .Select(c => c.DocumentId)
            .ToListAsync(ct);

        var query = _dbContext.Documents
            .AsNoTracking()
            .Where(d => d.Status != DocumentStatus.Trashed &&
                        (d.OwnerUserId == currentUserId || accessibleDocIds.Contains(d.Id)));

        if (projectId.HasValue)
        {
            query = query.Where(d => d.ProjectId == projectId.Value);
        }

        var docs = await query
            .OrderByDescending(d => d.UpdatedAtUtc)
            .Select(d => new
            {
                Document = d,
                OwnerName = d.OwnerUser != null && d.OwnerUser.Profile != null ? d.OwnerUser.Profile.DisplayName : (d.OwnerUser != null ? d.OwnerUser.UserName : "Author"),
                ProjectTitle = d.Project != null ? d.Project.Title : null,
                MainFileName = d.MainFile != null ? d.MainFile.FileName : null,
                FileCount = d.Files.Count,
                CollaboratorCount = d.Collaborators.Count
            })
            .ToListAsync(ct);

        return docs.Select(x => new DocumentSummaryDto(
            x.Document.Id,
            x.Document.Title,
            x.Document.Description,
            x.Document.DocumentType,
            x.Document.Mode,
            x.Document.ProjectId,
            x.ProjectTitle,
            x.Document.OwnerUserId,
            x.OwnerName ?? "Author",
            x.Document.Visibility,
            x.Document.Status,
            x.Document.MainFileId,
            x.MainFileName,
            x.Document.LastCompiledAtUtc,
            x.Document.LastCompilationStatus,
            x.Document.UpdatedAtUtc,
            x.Document.CreatedAtUtc,
            x.FileCount,
            x.CollaboratorCount
        )).ToList();
    }

    public async Task<DocumentDetailDto> UpdateDocumentAsync(
        Guid currentUserId,
        Guid documentId,
        UpdateDocumentRequest request,
        CancellationToken ct = default)
    {
        var document = await _dbContext.Documents
            .Include(d => d.OwnerUser)
                .ThenInclude(u => u!.Profile)
            .Include(d => d.Project)
            .Include(d => d.MainFile)
            .Include(d => d.Files)
            .Include(d => d.Collaborators)
                .ThenInclude(c => c.User)
                    .ThenInclude(u => u!.Profile)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.Status != DocumentStatus.Trashed, ct);

        if (document == null)
            throw new KeyNotFoundException($"Document with ID {documentId} was not found.");

        var isOwner = document.OwnerUserId == currentUserId;
        var isEditor = document.Collaborators.Any(c => c.UserId == currentUserId && c.IsActive() &&
                                                       (c.Role == DocumentPermissionRole.Owner || c.Role == DocumentPermissionRole.Editor));

        if (!isOwner && !isEditor)
            throw new UnauthorizedAccessException("You do not have permission to modify this document.");

        if (document.ConcurrencyStamp != request.ConcurrencyStamp)
            throw new DbUpdateConcurrencyException("The document has been modified by another collaborator. Please refresh and try again.");

        document.UpdateMetadata(request.Title, request.Description, request.Visibility, request.DocumentType);

        await _dbContext.SaveChangesAsync(ct);

        return (await GetDocumentByIdAsync(currentUserId, documentId, ct))!;
    }

    public async Task DeleteDocumentAsync(
        Guid currentUserId,
        Guid documentId,
        CancellationToken ct = default)
    {
        var document = await _dbContext.Documents
            .Include(d => d.Collaborators)
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);

        if (document == null)
            throw new KeyNotFoundException($"Document with ID {documentId} was not found.");

        var isOwner = document.OwnerUserId == currentUserId ||
                      document.Collaborators.Any(c => c.UserId == currentUserId && c.Role == DocumentPermissionRole.Owner && c.IsActive());

        if (!isOwner)
            throw new UnauthorizedAccessException("Only document owners can delete documents.");

        document.Trash();
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Document {DocumentId} trashed by user {UserId}.", documentId, currentUserId);
    }

    public async Task<CollaboratorDto> AddCollaboratorAsync(
        Guid currentUserId,
        Guid documentId,
        AddCollaboratorRequest request,
        CancellationToken ct = default)
    {
        var document = await _dbContext.Documents
            .Include(d => d.Collaborators)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.Status != DocumentStatus.Trashed, ct);

        if (document == null)
            throw new KeyNotFoundException($"Document with ID {documentId} was not found.");

        var isOwner = document.OwnerUserId == currentUserId ||
                      document.Collaborators.Any(c => c.UserId == currentUserId && c.Role == DocumentPermissionRole.Owner && c.IsActive());

        if (!isOwner)
            throw new UnauthorizedAccessException("Only document owners can manage collaborators.");

        var targetUser = await _dbContext.Users.AsNoTracking().Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (targetUser == null)
            throw new KeyNotFoundException($"User with ID {request.UserId} was not found.");

        var existing = document.Collaborators.FirstOrDefault(c => c.UserId == request.UserId);
        if (existing != null)
        {
            existing.Role = request.Role;
            existing.ExpiresAtUtc = request.ExpiresAtUtc;
            existing.RevokedAtUtc = null;
        }
        else
        {
            var newCollab = new DocumentCollaborator(documentId, request.UserId, request.Role, currentUserId, request.ExpiresAtUtc);
            _dbContext.DocumentCollaborators.Add(newCollab);
            existing = newCollab;
        }

        await _dbContext.SaveChangesAsync(ct);

        return new CollaboratorDto(
            existing.Id,
            existing.DocumentId,
            existing.UserId,
            targetUser.Profile?.DisplayName ?? targetUser.UserName ?? "Collaborator",
            targetUser.Email ?? string.Empty,
            existing.Role,
            existing.GrantedAtUtc,
            existing.ExpiresAtUtc,
            existing.IsActive(),
            existing.CanCompile,
            existing.CanReview
        );
    }

    public async Task<CollaboratorDto> UpdateCollaboratorAsync(
        Guid currentUserId,
        Guid documentId,
        Guid targetUserId,
        UpdateCollaboratorRoleRequest request,
        CancellationToken ct = default)
    {
        var document = await _dbContext.Documents
            .Include(d => d.Collaborators)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.Status != DocumentStatus.Trashed, ct);

        if (document == null)
            throw new KeyNotFoundException($"Document with ID {documentId} was not found.");

        var isOwner = document.OwnerUserId == currentUserId ||
                      document.Collaborators.Any(c => c.UserId == currentUserId && c.Role == DocumentPermissionRole.Owner && c.IsActive());

        if (!isOwner)
            throw new UnauthorizedAccessException("Only document owners can update collaborator roles.");

        if (document.OwnerUserId == targetUserId)
            throw new DomainException("Cannot modify the document primary owner role.");

        var existing = document.Collaborators.FirstOrDefault(c => c.UserId == targetUserId);
        if (existing == null)
            throw new KeyNotFoundException($"Collaborator with user ID {targetUserId} was not found on this document.");

        existing.Role = request.Role;
        existing.ExpiresAtUtc = request.ExpiresAtUtc;

        await _dbContext.SaveChangesAsync(ct);

        var targetUser = await _dbContext.Users.AsNoTracking().Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == targetUserId, ct);

        return new CollaboratorDto(
            existing.Id,
            existing.DocumentId,
            existing.UserId,
            targetUser?.Profile?.DisplayName ?? targetUser?.UserName ?? "Collaborator",
            targetUser?.Email ?? string.Empty,
            existing.Role,
            existing.GrantedAtUtc,
            existing.ExpiresAtUtc,
            existing.IsActive(),
            existing.CanCompile,
            existing.CanReview
        );
    }

    public async Task RemoveCollaboratorAsync(
        Guid currentUserId,
        Guid documentId,
        Guid targetUserId,
        CancellationToken ct = default)
    {
        var document = await _dbContext.Documents
            .Include(d => d.Collaborators)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.Status != DocumentStatus.Trashed, ct);

        if (document == null)
            throw new KeyNotFoundException($"Document with ID {documentId} was not found.");

        var isOwner = document.OwnerUserId == currentUserId ||
                      document.Collaborators.Any(c => c.UserId == currentUserId && c.Role == DocumentPermissionRole.Owner && c.IsActive());

        if (!isOwner && currentUserId != targetUserId)
            throw new UnauthorizedAccessException("Only document owners or the user themselves can remove a collaborator.");

        if (document.OwnerUserId == targetUserId)
            throw new DomainException("Cannot remove the primary document owner.");

        var existing = document.Collaborators.FirstOrDefault(c => c.UserId == targetUserId);
        if (existing != null)
        {
            _dbContext.DocumentCollaborators.Remove(existing);
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task<List<CollaboratorDto>> GetCollaboratorsAsync(
        Guid currentUserId,
        Guid documentId,
        CancellationToken ct = default)
    {
        var detail = await GetDocumentByIdAsync(currentUserId, documentId, ct);
        if (detail == null)
            throw new KeyNotFoundException($"Document with ID {documentId} was not found.");

        return detail.Collaborators;
    }

    private static string GenerateDefaultLatexTemplate(string title)
    {
        return @"\documentclass[11pt,a4paper]{article}
\usepackage[utf8]{inputenc}
\usepackage{amsmath,amsfonts,amssymb}
\usepackage{graphicx}
\usepackage{hyperref}

\title{" + title + @"}
\author{Pavena Research Studio}
\date{\today}

\begin{document}

\maketitle

\begin{abstract}
This research manuscript was authored in Pavena Research Studio. Write your abstract here.
\end{abstract}

\section{Introduction}
Introduce the research background, hypothesis, and primary contributions here.

\section{Methodology}
Detail the analytical, experimental, or computational methodology used.

\section{Results and Discussion}
Present empirical findings, tables, figures, and theoretical analysis.

\section{Conclusion}
Summarize findings, limitations, and future research directions.

\bibliographystyle{plain}
\begin{thebibliography}{9}
\bibitem{pavena2026}
Pavena Collaboration Network, \emph{Advancing Research with Integrity}, 2026.
\end{thebibliography}

\end{document}
";
    }
}
