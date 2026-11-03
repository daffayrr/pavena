using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pavena.Infrastructure.Persistence.Migrations.MySql
{
    /// <inheritdoc />
    public partial class AddMilestonePResearchProposalsMySql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. research_proposals
            migrationBuilder.CreateTable(
                name: "research_proposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ChallengeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LeadResearcherId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SubmittingOrganizationId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Title = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Slug = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Abstract = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProblemStatement = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResearchObjectives = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProposedMethodology = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpectedOutcomes = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProposedTimelineMonths = table.Column<int>(type: "int", nullable: false),
                    ProposedBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, defaultValue: "USD")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FundingJustification = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Confidentiality = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubmissionVersion = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ConvertedProjectId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    DecisionNotes = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DecisionByUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    DecisionAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EligibilityReviewedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EligibilityReviewedByUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    EligibilityRejectionReason = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnderReviewAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RevisionRequestedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RevisionGuidance = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResubmittedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_proposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_research_proposals_industry_challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "industry_challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_research_proposals_users_LeadResearcherId",
                        column: x => x.LeadResearcherId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_research_proposals_organizations_SubmittingOrganizationId",
                        column: x => x.SubmittingOrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_research_proposals_research_projects_ConvertedProjectId",
                        column: x => x.ConvertedProjectId,
                        principalTable: "research_projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_research_proposals_users_DecisionByUserId",
                        column: x => x.DecisionByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_research_proposals_users_EligibilityReviewedByUserId",
                        column: x => x.EligibilityReviewedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // 2. proposal_investigators
            migrationBuilder.CreateTable(
                name: "proposal_investigators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ProposalId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Role = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContributionDescription = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proposal_investigators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_proposal_investigators_research_proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "research_proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_proposal_investigators_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // 3. proposal_research_domains
            migrationBuilder.CreateTable(
                name: "proposal_research_domains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ProposalId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Domain = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proposal_research_domains", x => x.Id);
                    table.ForeignKey(
                        name: "FK_proposal_research_domains_research_proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "research_proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // 4. proposal_expertise
            migrationBuilder.CreateTable(
                name: "proposal_expertise",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ProposalId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ExpertiseTag = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proposal_expertise", x => x.Id);
                    table.ForeignKey(
                        name: "FK_proposal_expertise_research_proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "research_proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // 5. proposal_attachments
            migrationBuilder.CreateTable(
                name: "proposal_attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ProposalId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AttachmentType = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StoragePath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContentType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileHashSha256 = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UploadedByUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proposal_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_proposal_attachments_research_proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "research_proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_proposal_attachments_users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // 6. proposal_version_snapshots
            migrationBuilder.CreateTable(
                name: "proposal_version_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ProposalId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    SnapshotDataJson = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChangeSummary = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedByUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proposal_version_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_proposal_version_snapshots_research_proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "research_proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_proposal_version_snapshots_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // 7. proposal_review_assignments
            migrationBuilder.CreateTable(
                name: "proposal_review_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ProposalId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ReviewerUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AssignedByUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DueAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AssignedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AcceptedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeclinedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeclineReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HasCoiDeclaration = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CoiType = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CoiDescription = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CoiDeclaredAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proposal_review_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_proposal_review_assignments_research_proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "research_proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_proposal_review_assignments_users_ReviewerUserId",
                        column: x => x.ReviewerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_proposal_review_assignments_users_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // 8. proposal_assessments
            migrationBuilder.CreateTable(
                name: "proposal_assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AssignmentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ProposalId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ReviewerUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TechnicalMeritScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MethodologyScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    FeasibilityScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    InnovationScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    BudgetAdequacyScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TeamQualificationScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ImpactPotentialScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    AlignmentWithChallengeScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    OverallScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Strengths = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Weaknesses = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecommendationsForImprovement = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfidentialRemarksForSponsor = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Recommendation = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proposal_assessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_proposal_assessments_proposal_review_assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "proposal_review_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_proposal_assessments_research_proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "research_proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_proposal_assessments_users_ReviewerUserId",
                        column: x => x.ReviewerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // 9. proposal_activity_logs
            migrationBuilder.CreateTable(
                name: "proposal_activity_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ProposalId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ActorUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ActivityType = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MetadataJson = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proposal_activity_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_proposal_activity_logs_research_proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "research_proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_proposal_activity_logs_users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // Indexes
            migrationBuilder.CreateIndex(
                name: "IX_research_proposals_ChallengeId_Status",
                table: "research_proposals",
                columns: new[] { "ChallengeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_research_proposals_LeadResearcherId_Status",
                table: "research_proposals",
                columns: new[] { "LeadResearcherId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_research_proposals_SubmittingOrganizationId",
                table: "research_proposals",
                column: "SubmittingOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_research_proposals_Slug",
                table: "research_proposals",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_research_proposals_Status",
                table: "research_proposals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_research_proposals_ConvertedProjectId",
                table: "research_proposals",
                column: "ConvertedProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_research_proposals_DecisionByUserId",
                table: "research_proposals",
                column: "DecisionByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_research_proposals_EligibilityReviewedByUserId",
                table: "research_proposals",
                column: "EligibilityReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_proposal_investigators_ProposalId_UserId",
                table: "proposal_investigators",
                columns: new[] { "ProposalId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proposal_investigators_UserId",
                table: "proposal_investigators",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_proposal_research_domains_ProposalId_Domain",
                table: "proposal_research_domains",
                columns: new[] { "ProposalId", "Domain" });

            migrationBuilder.CreateIndex(
                name: "IX_proposal_expertise_ProposalId_ExpertiseTag",
                table: "proposal_expertise",
                columns: new[] { "ProposalId", "ExpertiseTag" });

            migrationBuilder.CreateIndex(
                name: "IX_proposal_attachments_ProposalId_AttachmentType",
                table: "proposal_attachments",
                columns: new[] { "ProposalId", "AttachmentType" });

            migrationBuilder.CreateIndex(
                name: "IX_proposal_attachments_UploadedByUserId",
                table: "proposal_attachments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_proposal_version_snapshots_ProposalId_VersionNumber",
                table: "proposal_version_snapshots",
                columns: new[] { "ProposalId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proposal_version_snapshots_CreatedByUserId",
                table: "proposal_version_snapshots",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_proposal_review_assignments_ProposalId_ReviewerUserId",
                table: "proposal_review_assignments",
                columns: new[] { "ProposalId", "ReviewerUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proposal_review_assignments_ReviewerUserId",
                table: "proposal_review_assignments",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_proposal_review_assignments_AssignedByUserId",
                table: "proposal_review_assignments",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_proposal_assessments_AssignmentId",
                table: "proposal_assessments",
                column: "AssignmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proposal_assessments_ProposalId_ReviewerUserId",
                table: "proposal_assessments",
                columns: new[] { "ProposalId", "ReviewerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_proposal_assessments_ReviewerUserId",
                table: "proposal_assessments",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_proposal_activity_logs_ProposalId_CreatedAtUtc",
                table: "proposal_activity_logs",
                columns: new[] { "ProposalId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_proposal_activity_logs_ActorUserId",
                table: "proposal_activity_logs",
                column: "ActorUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "proposal_activity_logs");
            migrationBuilder.DropTable(name: "proposal_assessments");
            migrationBuilder.DropTable(name: "proposal_review_assignments");
            migrationBuilder.DropTable(name: "proposal_version_snapshots");
            migrationBuilder.DropTable(name: "proposal_attachments");
            migrationBuilder.DropTable(name: "proposal_expertise");
            migrationBuilder.DropTable(name: "proposal_research_domains");
            migrationBuilder.DropTable(name: "proposal_investigators");
            migrationBuilder.DropTable(name: "research_proposals");
        }
    }
}
