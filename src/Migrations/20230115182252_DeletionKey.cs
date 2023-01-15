using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlobBin.Migrations
{
    /// <inheritdoc />
    public partial class DeletionKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeletionKey",
                table: "Pastes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeletionKey",
                table: "Files",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletionKey",
                table: "Pastes");

            migrationBuilder.DropColumn(
                name: "DeletionKey",
                table: "Files");
        }
    }
}
