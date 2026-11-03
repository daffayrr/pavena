using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pavena.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class AddMilestoneNDocumentsWorkspacePostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Alter documents table
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "documents",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentType",
                table: "documents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Manuscript");

            migrationBuilder.AddColumn<Guid>(
                name: "MainFileId",
                table: "documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRealTimeEnabled",
                table: "documents",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCompiledAtUtc",
                table: "documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastCompilationStatus",
                table: "documents",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_documents_project_status",
                table: "documents",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "idx_documents_owner_status",
                table: "documents",
                columns: new[] { "OwnerUserId", "Status" });

            // Alter document_collaborators table
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAtUtc",
                table: "document_collaborators",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAtUtc",
                table: "document_collaborators",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanCompile",
                table: "document_collaborators",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanReview",
                table: "document_collaborators",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // Create document_files table
            migrationBuilder.CreateTable(
                name: "document_files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileExtension = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsMain = table.Column<bool>(type: "boolean", nullable: false),
                    IsBinary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_document_files_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_document_files_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "uq_doc_file_path",
                table: "document_files",
                columns: new[] { "DocumentId", "Path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_files_CreatedByUserId",
                table: "document_files",
                column: "CreatedByUserId");

            // Add FK for documents.MainFileId -> document_files.Id
            migrationBuilder.AddForeignKey(
                name: "FK_documents_document_files_MainFileId",
                table: "documents",
                column: "MainFileId",
                principalTable: "document_files",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Create document_crdt_updates table
            migrationBuilder.CreateTable(
                name: "document_crdt_updates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: true),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    UpdateBlob = table.Column<byte[]>(type: "bytea", nullable: false),
                    ClientSessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_crdt_updates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_document_crdt_updates_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_document_crdt_updates_document_files_FileId",
                        column: x => x.FileId,
                        principalTable: "document_files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_document_crdt_updates_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_doc_crdt_seq",
                table: "document_crdt_updates",
                columns: new[] { "DocumentId", "SequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_document_crdt_updates_FileId",
                table: "document_crdt_updates",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_document_crdt_updates_CreatedByUserId",
                table: "document_crdt_updates",
                column: "CreatedByUserId");

            // Create document_snapshots table
            migrationBuilder.CreateTable(
                name: "document_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: true),
                    SnapshotSequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    CompactedThroughSequence = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotStorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ChecksumSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_document_snapshots_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_document_snapshots_document_files_FileId",
                        column: x => x.FileId,
                        principalTable: "document_files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_doc_snapshot_seq",
                table: "document_snapshots",
                columns: new[] { "DocumentId", "SnapshotSequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_document_snapshots_FileId",
                table: "document_snapshots",
                column: "FileId");

            // Create document_versions table
            migrationBuilder.CreateTable(
                name: "document_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    VersionSource = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ArchiveStorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ArchiveSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ChecksumSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsRestorable = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_document_versions_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_document_versions_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "uq_doc_version_num",
                table: "document_versions",
                columns: new[] { "DocumentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_versions_CreatedByUserId",
                table: "document_versions",
                column: "CreatedByUserId");

            // Create compilation_jobs table
            migrationBuilder.CreateTable(
                name: "compilation_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TriggeredByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Engine = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    QueuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    OutputLog = table.Column<string>(type: "text", nullable: true),
                    LogStorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PdfStorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PdfSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compilation_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_compilation_jobs_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_compilation_jobs_users_TriggeredByUserId",
                        column: x => x.TriggeredByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_compilation_jobs_doc_queued",
                table: "compilation_jobs",
                columns: new[] { "DocumentId", "QueuedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_compilation_jobs_TriggeredByUserId",
                table: "compilation_jobs",
                column: "TriggeredByUserId");

            // Create compilation_artifacts table
            migrationBuilder.CreateTable(
                name: "compilation_artifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompilationJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtifactType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compilation_artifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_compilation_artifacts_compilation_jobs_CompilationJobId",
                        column: x => x.CompilationJobId,
                        principalTable: "compilation_jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_compilation_artifacts_CompilationJobId",
                table: "compilation_artifacts",
                column: "CompilationJobId");

            // Create document_comments table
            migrationBuilder.CreateTable(
                name: "document_comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: true),
                    LineNumber = table.Column<int>(type: "integer", nullable: true),
                    SelectedText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ParentCommentId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_document_comments_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_document_comments_document_files_FileId",
                        column: x => x.FileId,
                        principalTable: "document_files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_document_comments_users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_document_comments_users_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_document_comments_document_comments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "document_comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_doc_comments_status",
                table: "document_comments",
                columns: new[] { "DocumentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_document_comments_AuthorUserId",
                table: "document_comments",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_document_comments_ResolvedByUserId",
                table: "document_comments",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_document_comments_ParentCommentId",
                table: "document_comments",
                column: "ParentCommentId");

            // Create document_reviews table
            migrationBuilder.CreateTable(
                name: "document_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SummaryNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_document_reviews_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_document_reviews_document_versions_DocumentVersionId",
                        column: x => x.DocumentVersionId,
                        principalTable: "document_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_document_reviews_users_ReviewerUserId",
                        column: x => x.ReviewerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_document_reviews_users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_doc_reviews_status",
                table: "document_reviews",
                columns: new[] { "DocumentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_document_reviews_DocumentVersionId",
                table: "document_reviews",
                column: "DocumentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_document_reviews_ReviewerUserId",
                table: "document_reviews",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_document_reviews_RequestedByUserId",
                table: "document_reviews",
                column: "RequestedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_documents_document_files_MainFileId",
                table: "documents");

            migrationBuilder.DropTable(name: "document_reviews");
            migrationBuilder.DropTable(name: "document_comments");
            migrationBuilder.DropTable(name: "compilation_artifacts");
            migrationBuilder.DropTable(name: "compilation_jobs");
            migrationBuilder.DropTable(name: "document_versions");
            migrationBuilder.DropTable(name: "document_snapshots");
            migrationBuilder.DropTable(name: "document_crdt_updates");
            migrationBuilder.DropTable(name: "document_files");

            migrationBuilder.DropIndex(
                name: "idx_documents_project_status",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "idx_documents_owner_status",
                table: "documents");

            migrationBuilder.DropColumn(name: "Description", table: "documents");
            migrationBuilder.DropColumn(name: "DocumentType", table: "documents");
            migrationBuilder.DropColumn(name: "MainFileId", table: "documents");
            migrationBuilder.DropColumn(name: "IsRealTimeEnabled", table: "documents");
            migrationBuilder.DropColumn(name: "LastCompiledAtUtc", table: "documents");
            migrationBuilder.DropColumn(name: "LastCompilationStatus", table: "documents");

            migrationBuilder.DropColumn(name: "ExpiresAtUtc", table: "document_collaborators");
            migrationBuilder.DropColumn(name: "RevokedAtUtc", table: "document_collaborators");
            migrationBuilder.DropColumn(name: "CanCompile", table: "document_collaborators");
            migrationBuilder.DropColumn(name: "CanReview", table: "document_collaborators");
        }
    }
}
