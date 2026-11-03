using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pavena.Domain.Entities.Documents;

namespace Pavena.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Title)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(d => d.Description)
            .HasMaxLength(1000);

        builder.Property(d => d.DocumentType)
            .HasMaxLength(50)
            .HasDefaultValue("Manuscript")
            .IsRequired();

        builder.Property(d => d.Mode)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.Visibility)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.LastCompilationStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(d => d.ConcurrencyStamp)
            .HasMaxLength(36)
            .IsConcurrencyToken();

        builder.HasOne(d => d.OwnerUser)
            .WithMany()
            .HasForeignKey(d => d.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Project)
            .WithMany()
            .HasForeignKey(d => d.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(d => d.MainFile)
            .WithMany()
            .HasForeignKey(d => d.MainFileId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(d => new { d.ProjectId, d.Status })
            .HasDatabaseName("idx_documents_project_status");

        builder.HasIndex(d => new { d.OwnerUserId, d.Status })
            .HasDatabaseName("idx_documents_owner_status");
    }
}

public class DocumentCollaboratorConfiguration : IEntityTypeConfiguration<DocumentCollaborator>
{
    public void Configure(EntityTypeBuilder<DocumentCollaborator> builder)
    {
        builder.ToTable("document_collaborators");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(c => new { c.DocumentId, c.UserId })
            .IsUnique()
            .HasDatabaseName("uq_doc_collaborators_pair");

        builder.HasOne(c => c.Document)
            .WithMany(d => d.Collaborators)
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.GrantedByUser)
            .WithMany()
            .HasForeignKey(c => c.GrantedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DocumentFileConfiguration : IEntityTypeConfiguration<DocumentFile>
{
    public void Configure(EntityTypeBuilder<DocumentFile> builder)
    {
        builder.ToTable("document_files");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Path)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(f => f.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(f => f.FileExtension)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(f => f.MimeType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(f => f.StorageKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(f => new { f.DocumentId, f.Path })
            .IsUnique()
            .HasDatabaseName("uq_doc_file_path");

        builder.HasOne(f => f.Document)
            .WithMany(d => d.Files)
            .HasForeignKey(f => f.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.CreatedByUser)
            .WithMany()
            .HasForeignKey(f => f.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DocumentCrdtUpdateConfiguration : IEntityTypeConfiguration<DocumentCrdtUpdate>
{
    public void Configure(EntityTypeBuilder<DocumentCrdtUpdate> builder)
    {
        builder.ToTable("document_crdt_updates");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.UpdateBlob)
            .IsRequired();

        builder.Property(u => u.ClientSessionId)
            .HasMaxLength(100);

        builder.HasIndex(u => new { u.DocumentId, u.SequenceNumber })
            .HasDatabaseName("idx_doc_crdt_seq");

        builder.HasOne(u => u.Document)
            .WithMany(d => d.CrdtUpdates)
            .HasForeignKey(u => u.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.File)
            .WithMany()
            .HasForeignKey(u => u.FileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.CreatedByUser)
            .WithMany()
            .HasForeignKey(u => u.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DocumentSnapshotConfiguration : IEntityTypeConfiguration<DocumentSnapshot>
{
    public void Configure(EntityTypeBuilder<DocumentSnapshot> builder)
    {
        builder.ToTable("document_snapshots");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SnapshotStorageKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.ChecksumSha256)
            .HasMaxLength(64);

        builder.HasIndex(s => new { s.DocumentId, s.SnapshotSequenceNumber })
            .HasDatabaseName("idx_doc_snapshot_seq");

        builder.HasOne(s => s.Document)
            .WithMany(d => d.Snapshots)
            .HasForeignKey(s => s.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.File)
            .WithMany()
            .HasForeignKey(s => s.FileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.ToTable("document_versions");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(v => v.Description)
            .HasMaxLength(1000);

        builder.Property(v => v.VersionSource)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(v => v.ArchiveStorageKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(v => v.ChecksumSha256)
            .HasMaxLength(64);

        builder.HasIndex(v => new { v.DocumentId, v.VersionNumber })
            .IsUnique()
            .HasDatabaseName("uq_doc_version_num");

        builder.HasOne(v => v.Document)
            .WithMany(d => d.Versions)
            .HasForeignKey(v => v.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.CreatedByUser)
            .WithMany()
            .HasForeignKey(v => v.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CompilationJobConfiguration : IEntityTypeConfiguration<CompilationJob>
{
    public void Configure(EntityTypeBuilder<CompilationJob> builder)
    {
        builder.ToTable("compilation_jobs");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.Engine)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(j => j.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(j => j.LogStorageKey)
            .HasMaxLength(500);

        builder.Property(j => j.PdfStorageKey)
            .HasMaxLength(500);

        builder.Property(j => j.ErrorMessage)
            .HasMaxLength(1000);

        builder.HasIndex(j => new { j.DocumentId, j.QueuedAtUtc })
            .HasDatabaseName("idx_compilation_jobs_doc_queued");

        builder.HasOne(j => j.Document)
            .WithMany(d => d.CompilationJobs)
            .HasForeignKey(j => j.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(j => j.TriggeredByUser)
            .WithMany()
            .HasForeignKey(j => j.TriggeredByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CompilationArtifactConfiguration : IEntityTypeConfiguration<CompilationArtifact>
{
    public void Configure(EntityTypeBuilder<CompilationArtifact> builder)
    {
        builder.ToTable("compilation_artifacts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ArtifactType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(a => a.StorageKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasOne(a => a.CompilationJob)
            .WithMany(j => j.Artifacts)
            .HasForeignKey(a => a.CompilationJobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DocumentCommentConfiguration : IEntityTypeConfiguration<DocumentComment>
{
    public void Configure(EntityTypeBuilder<DocumentComment> builder)
    {
        builder.ToTable("document_comments");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.SelectedText)
            .HasMaxLength(1000);

        builder.Property(c => c.Content)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(c => new { c.DocumentId, c.Status })
            .HasDatabaseName("idx_doc_comments_status");

        builder.HasOne(c => c.Document)
            .WithMany(d => d.Comments)
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.File)
            .WithMany()
            .HasForeignKey(c => c.FileId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.AuthorUser)
            .WithMany()
            .HasForeignKey(c => c.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ResolvedByUser)
            .WithMany()
            .HasForeignKey(c => c.ResolvedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.ParentComment)
            .WithMany(p => p.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DocumentReviewConfiguration : IEntityTypeConfiguration<DocumentReview>
{
    public void Configure(EntityTypeBuilder<DocumentReview> builder)
    {
        builder.ToTable("document_reviews");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.SummaryNotes)
            .HasMaxLength(2000);

        builder.HasIndex(r => new { r.DocumentId, r.Status })
            .HasDatabaseName("idx_doc_reviews_status");

        builder.HasOne(r => r.Document)
            .WithMany(d => d.Reviews)
            .HasForeignKey(r => r.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.DocumentVersion)
            .WithMany()
            .HasForeignKey(r => r.DocumentVersionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.ReviewerUser)
            .WithMany()
            .HasForeignKey(r => r.ReviewerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.RequestedByUser)
            .WithMany()
            .HasForeignKey(r => r.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
