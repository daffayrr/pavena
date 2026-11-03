using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pavena.Infrastructure.Persistence.Migrations.MySql
{
    /// <inheritdoc />
    public partial class AddMilestoneMResearchWorkspaceMySql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OriginType",
                table: "research_projects",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ResearcherInitiated")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ResearchObjectives",
                table: "research_projects",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "SourceChallengeId",
                table: "research_projects",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "SourceProposalId",
                table: "research_projects",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "Stage",
                table: "research_projects",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Concept")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_research_projects_vis_status_created",
                table: "research_projects",
                columns: new[] { "Visibility", "Status", "CreatedAtUtc" });

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "research_members",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "InvitedAtUtc",
                table: "research_members",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LeftAtUtc",
                table: "research_members",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "research_milestones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ProjectId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Title = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetDateUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedByUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AchievedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_milestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_research_milestones_research_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "research_projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_research_milestones_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_milestones_proj_target_date",
                table: "research_milestones",
                columns: new[] { "ProjectId", "TargetDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_research_milestones_CreatedByUserId",
                table: "research_milestones",
                column: "CreatedByUserId");

            migrationBuilder.AddColumn<Guid>(
                name: "MilestoneId",
                table: "research_tasks",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "idx_tasks_proj_status_due",
                table: "research_tasks",
                columns: new[] { "ProjectId", "Status", "DueDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_research_tasks_MilestoneId",
                table: "research_tasks",
                column: "MilestoneId");

            migrationBuilder.AddForeignKey(
                name: "FK_research_tasks_research_milestones_MilestoneId",
                table: "research_tasks",
                column: "MilestoneId",
                principalTable: "research_milestones",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.CreateTable(
                name: "project_invitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ProjectId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    InvitedUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    InvitedByUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RoleOffered = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Message = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RespondedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_invitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_invitations_research_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "research_projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_invitations_users_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_project_invitations_users_InvitedUserId",
                        column: x => x.InvitedUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_invitations_proj_user_status",
                table: "project_invitations",
                columns: new[] { "ProjectId", "InvitedUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_project_invitations_InvitedByUserId",
                table: "project_invitations",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_project_invitations_InvitedUserId",
                table: "project_invitations",
                column: "InvitedUserId");

            migrationBuilder.CreateTable(
                name: "project_activity_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ProjectId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ActorUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Action = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Details = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimestampUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_activity_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_activity_logs_research_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "research_projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_activity_logs_users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_activity_proj_timestamp",
                table: "project_activity_logs",
                columns: new[] { "ProjectId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_project_activity_logs_ActorUserId",
                table: "project_activity_logs",
                column: "ActorUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "project_activity_logs");
            migrationBuilder.DropTable(name: "project_invitations");
            migrationBuilder.DropForeignKey(name: "FK_research_tasks_research_milestones_MilestoneId", table: "research_tasks");
            migrationBuilder.DropIndex(name: "idx_tasks_proj_status_due", table: "research_tasks");
            migrationBuilder.DropIndex(name: "IX_research_tasks_MilestoneId", table: "research_tasks");
            migrationBuilder.DropColumn(name: "MilestoneId", table: "research_tasks");
            migrationBuilder.DropTable(name: "research_milestones");
            migrationBuilder.DropColumn(name: "Status", table: "research_members");
            migrationBuilder.DropColumn(name: "InvitedAtUtc", table: "research_members");
            migrationBuilder.DropColumn(name: "LeftAtUtc", table: "research_members");
            migrationBuilder.DropIndex(name: "idx_research_projects_vis_status_created", table: "research_projects");
            migrationBuilder.DropColumn(name: "OriginType", table: "research_projects");
            migrationBuilder.DropColumn(name: "ResearchObjectives", table: "research_projects");
            migrationBuilder.DropColumn(name: "SourceChallengeId", table: "research_projects");
            migrationBuilder.DropColumn(name: "SourceProposalId", table: "research_projects");
            migrationBuilder.DropColumn(name: "Stage", table: "research_projects");
        }
    }
}
