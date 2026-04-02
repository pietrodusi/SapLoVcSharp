using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SapLoVcSharp.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductMatrixSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowCharacteristicClassName",
                table: "ProductMatrixSheets");

            migrationBuilder.DropColumn(
                name: "RowCharacteristicName",
                table: "ProductMatrixSheets");

            migrationBuilder.DropColumn(
                name: "RowCharacteristicValue",
                table: "ProductMatrixRows");

            migrationBuilder.AddColumn<string>(
                name: "RowLabel",
                table: "ProductMatrixRows",
                type: "TEXT",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColumnStyle",
                table: "ProductMatrixColumns",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ColumnValue",
                table: "ProductMatrixColumns",
                type: "TEXT",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowLabel",
                table: "ProductMatrixRows");

            migrationBuilder.DropColumn(
                name: "ColumnStyle",
                table: "ProductMatrixColumns");

            migrationBuilder.DropColumn(
                name: "ColumnValue",
                table: "ProductMatrixColumns");

            migrationBuilder.AddColumn<string>(
                name: "RowCharacteristicClassName",
                table: "ProductMatrixSheets",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RowCharacteristicName",
                table: "ProductMatrixSheets",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RowCharacteristicValue",
                table: "ProductMatrixRows",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }
    }
}
