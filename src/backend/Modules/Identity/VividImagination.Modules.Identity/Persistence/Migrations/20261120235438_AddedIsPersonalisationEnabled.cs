using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VividImagination.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddedIsPersonalisationEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPersonalisationEnabled",
                schema: "identity",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPersonalisationEnabled",
                schema: "identity",
                table: "AspNetUsers");
        }
    }
}
