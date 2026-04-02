using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SapLoVcSharp.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddVariantTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VariantTables",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariantTables", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "VariantTableColumns",
                columns: table => new
                {
                    TableName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ColumnIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacteristicName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ClassName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsKeyColumn = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariantTableColumns", x => new { x.TableName, x.ColumnIndex });
                    table.ForeignKey(
                        name: "FK_VariantTableColumns_VariantTables_TableName",
                        column: x => x.TableName,
                        principalTable: "VariantTables",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "IX_VariantTableColumns_TableName_ColumnIndex",
                table: "VariantTableColumns",
                columns: new[] { "TableName", "ColumnIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_VariantTableRows_TableName_RowIndex",
                table: "VariantTableRows",
                columns: new[] { "TableName", "RowIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_VariantTables_Status",
                table: "VariantTables",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VariantTableCells");

            migrationBuilder.DropTable(
                name: "VariantTableColumns");

            migrationBuilder.DropTable(
                name: "VariantTableRows");

            migrationBuilder.DropTable(
                name: "VariantTables");
        }
    }
}
