using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlobBin.Migrations
{
    /// <inheritdoc />
    public partial class ProbsEncrypted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsProbablyEncrypted",
                table: "Pastes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsProbablyEncrypted",
                table: "Files",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsProbablyEncrypted",
                table: "Pastes");

            migrationBuilder.DropColumn(
                name: "IsProbablyEncrypted",
                table: "Files");
        }
    }
}
