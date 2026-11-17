using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VividImagination.Modules.Social.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSocial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "social");

            migrationBuilder.CreateTable(
                name: "reports",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PhotoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterUserId = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "votes",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PhotoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false),
                    VotedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_votes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reports_PhotoId_ReporterUserId",
                schema: "social",
                table: "reports",
                columns: new[] { "PhotoId", "ReporterUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_votes_PhotoId_UserId",
                schema: "social",
                table: "votes",
                columns: new[] { "PhotoId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reports",
                schema: "social");

            migrationBuilder.DropTable(
                name: "votes",
                schema: "social");
        }
    }
}
