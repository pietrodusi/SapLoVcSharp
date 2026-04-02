using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SapLoVcSharp.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddLongDescriptionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LongDescription",
                table: "CharacteristicValues",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LongDescription",
                table: "Characteristics",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LongDescription",
                table: "CharacteristicValues");

            migrationBuilder.DropColumn(
                name: "LongDescription",
                table: "Characteristics");
        }
    }
}
