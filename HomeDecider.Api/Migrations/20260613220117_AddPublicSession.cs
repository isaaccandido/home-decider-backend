using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeDecider.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicPasswordHash",
                table: "Decisions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicToken",
                table: "Decisions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicPasswordHash",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "PublicToken",
                table: "Decisions");
        }
    }
}
