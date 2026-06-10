using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeDecider.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVoteSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Votes_DecisionId_VoterName",
                table: "Votes");

            migrationBuilder.AddColumn<bool>(
                name: "AllowMultipleVotes",
                table: "Decisions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymous",
                table: "Decisions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Votes_DecisionId_VoterName_OptionId",
                table: "Votes",
                columns: new[] { "DecisionId", "VoterName", "OptionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Votes_DecisionId_VoterName_OptionId",
                table: "Votes");

            migrationBuilder.DropColumn(
                name: "AllowMultipleVotes",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "IsAnonymous",
                table: "Decisions");

            migrationBuilder.CreateIndex(
                name: "IX_Votes_DecisionId_VoterName",
                table: "Votes",
                columns: new[] { "DecisionId", "VoterName" },
                unique: true);
        }
    }
}
