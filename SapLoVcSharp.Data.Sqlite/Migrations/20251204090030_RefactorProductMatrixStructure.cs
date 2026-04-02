using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SapLoVcSharp.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class RefactorProductMatrixStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductMatrixCells_ProductMatrixColumns_MatrixName_SheetIndex_ColumnIndex",
                table: "ProductMatrixCells");

            migrationBuilder.DropTable(
                name: "ProductMatrixColumns");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductMatrixCells",
                table: "ProductMatrixCells");

            migrationBuilder.DropIndex(
                name: "IX_ProductMatrixCells_MatrixName_SheetIndex_ColumnIndex",
                table: "ProductMatrixCells");

            migrationBuilder.RenameColumn(
                name: "ColumnIndex",
                table: "ProductMatrixCells",
                newName: "ValueIndex");

            migrationBuilder.AddColumn<int>(
                name: "CharacteristicIndex",
                table: "ProductMatrixCells",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductMatrixCells",
                table: "ProductMatrixCells",
                columns: new[] { "MatrixName", "SheetIndex", "RowIndex", "CharacteristicIndex", "ValueIndex" });

            migrationBuilder.CreateTable(
                name: "ProductMatrixCharacteristics",
                columns: table => new
                {
                    MatrixName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SheetIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacteristicIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacteristicName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ClassName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DisplayMode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ResultCharacteristicName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ResultClassName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductMatrixCharacteristics", x => new { x.MatrixName, x.SheetIndex, x.CharacteristicIndex });
                    table.ForeignKey(
                        name: "FK_ProductMatrixCharacteristics_ProductMatrixSheets_MatrixName_SheetIndex",
                        columns: x => new { x.MatrixName, x.SheetIndex },
                        principalTable: "ProductMatrixSheets",
                        principalColumns: new[] { "MatrixName", "SheetIndex" },
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductMatrixCharacteristics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductMatrixCells",
                table: "ProductMatrixCells");

            migrationBuilder.DropColumn(
                name: "CharacteristicIndex",
                table: "ProductMatrixCells");

            migrationBuilder.RenameColumn(
                name: "ValueIndex",
                table: "ProductMatrixCells",
                newName: "ColumnIndex");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductMatrixCells",
                table: "ProductMatrixCells",
                columns: new[] { "MatrixName", "SheetIndex", "RowIndex", "ColumnIndex" });

            migrationBuilder.CreateTable(
                name: "ProductMatrixColumns",
                columns: table => new
                {
                    MatrixName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SheetIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    ColumnIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    AvailableValuesJson = table.Column<string>(type: "TEXT", nullable: false),
                    CharacteristicName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ClassName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ColumnLabel = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ColumnMode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ColumnStyle = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ColumnValue = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    GroupHeader = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ResultCharacteristicName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ResultClassName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_ProductMatrixCells_MatrixName_SheetIndex_ColumnIndex",
                table: "ProductMatrixCells",
                columns: new[] { "MatrixName", "SheetIndex", "ColumnIndex" });

            migrationBuilder.AddForeignKey(
                name: "FK_ProductMatrixCells_ProductMatrixColumns_MatrixName_SheetIndex_ColumnIndex",
                table: "ProductMatrixCells",
                columns: new[] { "MatrixName", "SheetIndex", "ColumnIndex" },
                principalTable: "ProductMatrixColumns",
                principalColumns: new[] { "MatrixName", "SheetIndex", "ColumnIndex" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
