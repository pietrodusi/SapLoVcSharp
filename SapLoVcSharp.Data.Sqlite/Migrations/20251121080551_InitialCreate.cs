using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SapLoVcSharp.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Classes",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ClassType = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classes", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "Dependencies",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SourceCode = table.Column<string>(type: "TEXT", nullable: false),
                    AstJson = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dependencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DependencyId = table.Column<string>(type: "TEXT", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ContextJson = table.Column<string>(type: "TEXT", nullable: false),
                    ResultJson = table.Column<string>(type: "TEXT", nullable: false),
                    DurationMs = table.Column<long>(type: "INTEGER", nullable: false),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsConsistent = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Materials",
                columns: table => new
                {
                    MaterialNumber = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    MaterialType = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    BaseUnit = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ConfigurationProfileId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materials", x => x.MaterialNumber);
                });

            migrationBuilder.CreateTable(
                name: "Characteristics",
                columns: table => new
                {
                    ClassName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DataType = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Length = table.Column<int>(type: "INTEGER", nullable: false),
                    DecimalPlaces = table.Column<int>(type: "INTEGER", nullable: false),
                    IsMultiValue = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsRestrictable = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDisplayOnly = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characteristics", x => new { x.ClassName, x.Name });
                    table.ForeignKey(
                        name: "FK_Characteristics_Classes_ClassName",
                        column: x => x.ClassName,
                        principalTable: "Classes",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationProfiles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    MaterialNumber = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfigurationProfiles_Materials_MaterialNumber",
                        column: x => x.MaterialNumber,
                        principalTable: "Materials",
                        principalColumn: "MaterialNumber",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialClassAssignments",
                columns: table => new
                {
                    MaterialNumber = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ClassName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialClassAssignments", x => new { x.MaterialNumber, x.ClassName });
                    table.ForeignKey(
                        name: "FK_MaterialClassAssignments_Classes_ClassName",
                        column: x => x.ClassName,
                        principalTable: "Classes",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialClassAssignments_Materials_MaterialNumber",
                        column: x => x.MaterialNumber,
                        principalTable: "Materials",
                        principalColumn: "MaterialNumber",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacteristicValues",
                columns: table => new
                {
                    ClassName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CharacteristicName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacteristicValues", x => new { x.ClassName, x.CharacteristicName, x.Value });
                    table.ForeignKey(
                        name: "FK_CharacteristicValues_Characteristics_ClassName_CharacteristicName",
                        columns: x => new { x.ClassName, x.CharacteristicName },
                        principalTable: "Characteristics",
                        principalColumns: new[] { "ClassName", "Name" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationProfileProcedures",
                columns: table => new
                {
                    ConfigurationProfileId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DependencyId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SequenceNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationProfileProcedures", x => new { x.ConfigurationProfileId, x.DependencyId });
                    table.ForeignKey(
                        name: "FK_ConfigurationProfileProcedures_ConfigurationProfiles_ConfigurationProfileId",
                        column: x => x.ConfigurationProfileId,
                        principalTable: "ConfigurationProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConfigurationProfileProcedures_Dependencies_DependencyId",
                        column: x => x.DependencyId,
                        principalTable: "Dependencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DependencyNets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ConfigurationProfileId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DependencyNets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DependencyNets_ConfigurationProfiles_ConfigurationProfileId",
                        column: x => x.ConfigurationProfileId,
                        principalTable: "ConfigurationProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValueDependencies",
                columns: table => new
                {
                    ClassName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CharacteristicName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CharacteristicValue = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    DependencyId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DependencyType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValueDependencies", x => new { x.ClassName, x.CharacteristicName, x.CharacteristicValue, x.DependencyId });
                    table.ForeignKey(
                        name: "FK_ValueDependencies_CharacteristicValues_ClassName_CharacteristicName_CharacteristicValue",
                        columns: x => new { x.ClassName, x.CharacteristicName, x.CharacteristicValue },
                        principalTable: "CharacteristicValues",
                        principalColumns: new[] { "ClassName", "CharacteristicName", "Value" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ValueDependencies_Dependencies_DependencyId",
                        column: x => x.DependencyId,
                        principalTable: "Dependencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DependencyNetConstraints",
                columns: table => new
                {
                    DependencyNetId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DependencyId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SequenceNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DependencyNetConstraints", x => new { x.DependencyNetId, x.DependencyId });
                    table.ForeignKey(
                        name: "FK_DependencyNetConstraints_Dependencies_DependencyId",
                        column: x => x.DependencyId,
                        principalTable: "Dependencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DependencyNetConstraints_DependencyNets_DependencyNetId",
                        column: x => x.DependencyNetId,
                        principalTable: "DependencyNets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Characteristics_Name",
                table: "Characteristics",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CharacteristicValues_Value",
                table: "CharacteristicValues",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_ClassType",
                table: "Classes",
                column: "ClassType");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_Status",
                table: "Classes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationProfileProcedures_ConfigurationProfileId",
                table: "ConfigurationProfileProcedures",
                column: "ConfigurationProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationProfileProcedures_DependencyId",
                table: "ConfigurationProfileProcedures",
                column: "DependencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationProfileProcedures_SequenceNumber",
                table: "ConfigurationProfileProcedures",
                column: "SequenceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationProfiles_MaterialNumber",
                table: "ConfigurationProfiles",
                column: "MaterialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dependencies_Name",
                table: "Dependencies",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Dependencies_Status",
                table: "Dependencies",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Dependencies_Type",
                table: "Dependencies",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_DependencyNetConstraints_DependencyId",
                table: "DependencyNetConstraints",
                column: "DependencyId");

            migrationBuilder.CreateIndex(
                name: "IX_DependencyNetConstraints_DependencyNetId",
                table: "DependencyNetConstraints",
                column: "DependencyNetId");

            migrationBuilder.CreateIndex(
                name: "IX_DependencyNetConstraints_SequenceNumber",
                table: "DependencyNetConstraints",
                column: "SequenceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_DependencyNets_ConfigurationProfileId",
                table: "DependencyNets",
                column: "ConfigurationProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_DependencyNets_Status",
                table: "DependencyNets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionLogs_DependencyId",
                table: "ExecutionLogs",
                column: "DependencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionLogs_ExecutedAt",
                table: "ExecutionLogs",
                column: "ExecutedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialClassAssignments_ClassName",
                table: "MaterialClassAssignments",
                column: "ClassName");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_MaterialType",
                table: "Materials",
                column: "MaterialType");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_Status",
                table: "Materials",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ValueDependencies_DependencyId",
                table: "ValueDependencies",
                column: "DependencyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfigurationProfileProcedures");

            migrationBuilder.DropTable(
                name: "DependencyNetConstraints");

            migrationBuilder.DropTable(
                name: "ExecutionLogs");

            migrationBuilder.DropTable(
                name: "MaterialClassAssignments");

            migrationBuilder.DropTable(
                name: "ValueDependencies");

            migrationBuilder.DropTable(
                name: "DependencyNets");

            migrationBuilder.DropTable(
                name: "CharacteristicValues");

            migrationBuilder.DropTable(
                name: "Dependencies");

            migrationBuilder.DropTable(
                name: "ConfigurationProfiles");

            migrationBuilder.DropTable(
                name: "Characteristics");

            migrationBuilder.DropTable(
                name: "Materials");

            migrationBuilder.DropTable(
                name: "Classes");
        }
    }
}
