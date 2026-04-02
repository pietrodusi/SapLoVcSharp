using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SapLoVcSharp.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacteristicIsHiddenAndSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VariantTableCells");

            migrationBuilder.DropTable(
                name: "VariantTableRows");

            migrationBuilder.AddColumn<string>(
                name: "DatabaseTableName",
                table: "VariantTables",
                type: "TEXT",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsTableCreated",
                table: "VariantTables",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RowCount",
                table: "VariantTables",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SqlDataType",
                table: "VariantTableColumns",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "Characteristics",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Characteristics",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_VariantTables_DatabaseTableName",
                table: "VariantTables",
                column: "DatabaseTableName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VariantTables_DatabaseTableName",
                table: "VariantTables");

            migrationBuilder.DropColumn(
                name: "DatabaseTableName",
                table: "VariantTables");

            migrationBuilder.DropColumn(
                name: "IsTableCreated",
                table: "VariantTables");

            migrationBuilder.DropColumn(
                name: "RowCount",
                table: "VariantTables");

            migrationBuilder.DropColumn(
                name: "SqlDataType",
                table: "VariantTableColumns");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "Characteristics");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Characteristics");

            migrationBuilder.CreateTable(
                name: "VariantTableRows",
                columns: table => new
                {
                    TableName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RowIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariantTableRows", x => new { x.TableName, x.RowIndex });
                    table.ForeignKey(
                        name: "FK_VariantTableRows_VariantTables_TableName",
                        column: x => x.TableName,
                        principalTable: "VariantTables",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VariantTableCells",
                columns: table => new
                {
                    TableName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RowIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    ColumnIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariantTableCells", x => new { x.TableName, x.RowIndex, x.ColumnIndex });
                    table.ForeignKey(
                        name: "FK_VariantTableCells_VariantTableRows_TableName_RowIndex",
                        columns: x => new { x.TableName, x.RowIndex },
                        principalTable: "VariantTableRows",
                        principalColumns: new[] { "TableName", "RowIndex" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VariantTableCells_TableName_RowIndex_ColumnIndex",
                table: "VariantTableCells",
                columns: new[] { "TableName", "RowIndex", "ColumnIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_VariantTableRows_TableName_RowIndex",
                table: "VariantTableRows",
                columns: new[] { "TableName", "RowIndex" });
        }
    }
}
