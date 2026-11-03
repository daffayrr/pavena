using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pavena.Infrastructure.Persistence.Migrations.MySql
{
    /// <inheritdoc />
    public partial class AddMilestoneOIndustryChallengesMySql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. industry_challenges table
            migrationBuilder.CreateTable(
                name: "industry_challenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    OrganizationId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedByUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Title = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Slug = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Summary = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProblemStatement = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResearchScope = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpectedOutcomes = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Visibility = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Eligibility = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Currency = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true, defaultValue: "USD")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MinBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SubmissionDeadlineUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EstimatedProjectDurationMonths = table.Column<int>(type: "int", nullable: true),
                    RequiresManualNdaApproval = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    NdaTermsVersion = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "v1.0")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NdaTermsText = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CancellationReason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_industry_challenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_industry_challenges_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_industry_challenges_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // 2. challenge_attachments table
            migrationBuilder.CreateTable(
                name: "challenge_attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ChallengeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FileName = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileExtension = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MimeType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfidentialityLevel = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UploadedByUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_challenge_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_challenge_attachments_industry_challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "industry_challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_challenge_attachments_users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // 3. challenge_research_domains table
            migrationBuilder.CreateTable(
                name: "challenge_research_domains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ChallengeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DomainName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubDomain = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsPrimary = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_challenge_research_domains", x => x.Id);
                    table.ForeignKey(
                        name: "FK_challenge_research_domains_industry_challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "industry_challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // 4. challenge_expertise_requirements table
            migrationBuilder.CreateTable(
                name: "challenge_expertise_requirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ChallengeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SkillOrDiscipline = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImportanceLevel = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_challenge_expertise_requirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_challenge_expertise_requirements_industry_challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "industry_challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // 5. challenge_approvals table
            migrationBuilder.CreateTable(
                name: "challenge_approvals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ChallengeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ReviewerUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Decision = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReviewComments = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_challenge_approvals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_challenge_approvals_industry_challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "industry_challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_challenge_approvals_users_ReviewerUserId",
                        column: x => x.ReviewerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // 6. challenge_activity_logs table
            migrationBuilder.CreateTable(
                name: "challenge_activity_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ChallengeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ActorUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ActionType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreviousStatus = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewStatus = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimestampUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_challenge_activity_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_challenge_activity_logs_industry_challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "industry_challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_challenge_activity_logs_users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // 7. challenge_nda_agreements table
            migrationBuilder.CreateTable(
                name: "challenge_nda_agreements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ChallengeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ResearcherUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ResearcherOrganizationId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    NdaVersion = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TermsContentHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IpAddress = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgreedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsApprovedBySponsor = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_challenge_nda_agreements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_challenge_nda_agreements_industry_challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "industry_challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_challenge_nda_agreements_users_ResearcherUserId",
                        column: x => x.ResearcherUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_challenge_nda_agreements_users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // Indexes
            migrationBuilder.CreateIndex(
                name: "IX_industry_challenges_Slug",
                table: "industry_challenges",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_challenges_org_status",
                table: "industry_challenges",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "idx_challenges_vis_status_deadline",
                table: "industry_challenges",
                columns: new[] { "Visibility", "Status", "SubmissionDeadlineUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_industry_challenges_CreatedByUserId",
                table: "industry_challenges",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "idx_ch_att_confidentiality",
                table: "challenge_attachments",
                columns: new[] { "ChallengeId", "ConfidentialityLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_challenge_attachments_UploadedByUserId",
                table: "challenge_attachments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "idx_ch_domain",
                table: "challenge_research_domains",
                columns: new[] { "ChallengeId", "DomainName" });

            migrationBuilder.CreateIndex(
                name: "idx_ch_expertise",
                table: "challenge_expertise_requirements",
                columns: new[] { "ChallengeId", "SkillOrDiscipline" });

            migrationBuilder.CreateIndex(
                name: "idx_ch_approvals_decision",
                table: "challenge_approvals",
                columns: new[] { "ChallengeId", "Decision" });

            migrationBuilder.CreateIndex(
                name: "IX_challenge_approvals_ReviewerUserId",
                table: "challenge_approvals",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "idx_ch_activity_time",
                table: "challenge_activity_logs",
                columns: new[] { "ChallengeId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_challenge_activity_logs_ActorUserId",
                table: "challenge_activity_logs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "idx_ch_nda_unique",
                table: "challenge_nda_agreements",
                columns: new[] { "ChallengeId", "ResearcherUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_ch_nda_approved",
                table: "challenge_nda_agreements",
                columns: new[] { "ChallengeId", "IsApprovedBySponsor" });

            migrationBuilder.CreateIndex(
                name: "IX_challenge_nda_agreements_ApprovedByUserId",
                table: "challenge_nda_agreements",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_challenge_nda_agreements_ResearcherUserId",
                table: "challenge_nda_agreements",
                column: "ResearcherUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "challenge_nda_agreements");
            migrationBuilder.DropTable(name: "challenge_activity_logs");
            migrationBuilder.DropTable(name: "challenge_approvals");
            migrationBuilder.DropTable(name: "challenge_expertise_requirements");
            migrationBuilder.DropTable(name: "challenge_research_domains");
            migrationBuilder.DropTable(name: "challenge_attachments");
            migrationBuilder.DropTable(name: "industry_challenges");
        }
    }
}
