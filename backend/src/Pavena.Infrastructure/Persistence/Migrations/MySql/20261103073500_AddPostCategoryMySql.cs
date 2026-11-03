using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pavena.Infrastructure.Persistence.Migrations.MySql
{
    /// <inheritdoc />
    public partial class AddPostCategoryMySql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "posts",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "GeneralAcademic")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_posts_category_status_published",
                table: "posts",
                columns: new[] { "Category", "Status", "PublishedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_posts_category_status_published",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "posts");
        }
    }
}
