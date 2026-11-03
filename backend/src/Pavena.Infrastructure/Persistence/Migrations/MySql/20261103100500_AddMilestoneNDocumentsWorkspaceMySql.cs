using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pavena.Infrastructure.Persistence.Migrations.MySql
{
    /// <inheritdoc />
    public partial class AddMilestoneNDocumentsWorkspaceMySql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Alter documents table
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "documents",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DocumentType",
                table: "documents",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Manuscript")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "MainFileId",
                table: "documents",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<bool>(
                name: "IsRealTimeEnabled",
                table: "documents",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCompiledAtUtc",
                table: "documents",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastCompilationStatus",
                table: "documents",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

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
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAtUtc",
                table: "document_collaborators",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanCompile",
                table: "document_collaborators",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanReview",
                table: "document_collaborators",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            // Create document_files table
            migrationBuilder.CreateTable(
                name: "document_files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DocumentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Path = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileExtension = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MimeType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsMain = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsBinary = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DocumentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FileId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    UpdateBlob = table.Column<byte[]>(type: "longblob", nullable: false),
                    ClientSessionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedByUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DocumentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FileId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    SnapshotSequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    CompactedThroughSequence = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotStorageKey = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ChecksumSha256 = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DocumentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VersionSource = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ArchiveStorageKey = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ArchiveSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ChecksumSha256 = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsRestorable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DocumentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TriggeredByUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Engine = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QueuedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DurationMs = table.Column<int>(type: "int", nullable: true),
                    OutputLog = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LogStorageKey = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PdfStorageKey = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PdfSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    ErrorMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CompilationJobId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ArtifactType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StorageKey = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_compilation_artifacts_CompilationJobId",
                table: "compilation_artifacts",
                column: "CompilationJobId");

            // Create document_comments table
            migrationBuilder.CreateTable(
                name: "document_comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DocumentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FileId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    LineNumber = table.Column<int>(type: "int", nullable: true),
                    SelectedText = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParentCommentId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    AuthorUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DocumentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DocumentVersionId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ReviewerUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RequestedByUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SummaryNotes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
