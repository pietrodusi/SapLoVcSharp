using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SapLoVcSharp.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddProductMatrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductMatrices",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    MaterialNumber = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    GeneratedVariantTableName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductMatrices", x => x.Name);
                    table.ForeignKey(
                        name: "FK_ProductMatrices_Materials_MaterialNumber",
                        column: x => x.MaterialNumber,
                        principalTable: "Materials",
                        principalColumn: "MaterialNumber",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductMatrices_VariantTables_GeneratedVariantTableName",
                        column: x => x.GeneratedVariantTableName,
                        principalTable: "VariantTables",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProductMatrixSheets",
                columns: table => new
                {
                    MatrixName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SheetIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    RowCharacteristicName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RowCharacteristicClassName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductMatrixSheets", x => new { x.MatrixName, x.SheetIndex });
                    table.ForeignKey(
                        name: "FK_ProductMatrixSheets_ProductMatrices_MatrixName",
                        column: x => x.MatrixName,
                        principalTable: "ProductMatrices",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductMatrixColumns",
                columns: table => new
                {
                    MatrixName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SheetIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    ColumnIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    GroupHeader = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ColumnLabel = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    CharacteristicName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ClassName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ColumnMode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ResultCharacteristicName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ResultClassName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AvailableValuesJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductMatrixColumns", x => new { x.MatrixName, x.SheetIndex, x.ColumnIndex });
                    table.ForeignKey(
                        name: "FK_ProductMatrixColumns_ProductMatrixSheets_MatrixName_SheetIndex",
                        columns: x => new { x.MatrixName, x.SheetIndex },
                        principalTable: "ProductMatrixSheets",
                        principalColumns: new[] { "MatrixName", "SheetIndex" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductMatrixRows",
                columns: table => new
                {
                    MatrixName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SheetIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    RowIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    RowCharacteristicValue = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductMatrixRows", x => new { x.MatrixName, x.SheetIndex, x.RowIndex });
                    table.ForeignKey(
                        name: "FK_ProductMatrixRows_ProductMatrixSheets_MatrixName_SheetIndex",
                        columns: x => new { x.MatrixName, x.SheetIndex },
                        principalTable: "ProductMatrixSheets",
                        principalColumns: new[] { "MatrixName", "SheetIndex" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductMatrixCells",
                columns: table => new
                {
                    MatrixName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SheetIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    RowIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    ColumnIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductMatrixCells", x => new { x.MatrixName, x.SheetIndex, x.RowIndex, x.ColumnIndex });
                    table.ForeignKey(
                        name: "FK_ProductMatrixCells_ProductMatrixColumns_MatrixName_SheetIndex_ColumnIndex",
                        columns: x => new { x.MatrixName, x.SheetIndex, x.ColumnIndex },
                        principalTable: "ProductMatrixColumns",
                        principalColumns: new[] { "MatrixName", "SheetIndex", "ColumnIndex" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductMatrixCells_ProductMatrixRows_MatrixName_SheetIndex_RowIndex",
                        columns: x => new { x.MatrixName, x.SheetIndex, x.RowIndex },
                        principalTable: "ProductMatrixRows",
                        principalColumns: new[] { "MatrixName", "SheetIndex", "RowIndex" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductMatrices_GeneratedVariantTableName",
                table: "ProductMatrices",
                column: "GeneratedVariantTableName");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMatrices_MaterialNumber",
                table: "ProductMatrices",
                column: "MaterialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMatrices_Status",
                table: "ProductMatrices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMatrixCells_MatrixName_SheetIndex_ColumnIndex",
                table: "ProductMatrixCells",
                columns: new[] { "MatrixName", "SheetIndex", "ColumnIndex" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductMatrixCells");

            migrationBuilder.DropTable(
                name: "ProductMatrixColumns");

            migrationBuilder.DropTable(
                name: "ProductMatrixRows");

            migrationBuilder.DropTable(
                name: "ProductMatrixSheets");

            migrationBuilder.DropTable(
                name: "ProductMatrices");
        }
    }
}
