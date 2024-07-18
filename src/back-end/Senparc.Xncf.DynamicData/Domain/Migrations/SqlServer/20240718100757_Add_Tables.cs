using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.DynamicData.Domain.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class Add_Tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_DynamicData_TableMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TableName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_DynamicData_TableMetadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_DynamicData_ColumnMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TableMetadataId = table.Column<int>(type: "int", nullable: false),
                    ColumnName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ColumnType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsNullable = table.Column<bool>(type: "bit", nullable: false),
                    DefaultValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_DynamicData_ColumnMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Senparc_DynamicData_ColumnMetadata_Senparc_DynamicData_TableMetadata_TableMetadataId",
                        column: x => x.TableMetadataId,
                        principalTable: "Senparc_DynamicData_TableMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_DynamicData_TableData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TableId = table.Column<int>(type: "int", nullable: false),
                    ColumnMetadataId = table.Column<int>(type: "int", nullable: false),
                    CellValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TableMetadataId = table.Column<int>(type: "int", nullable: true),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_DynamicData_TableData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Senparc_DynamicData_TableData_Senparc_DynamicData_ColumnMetadata_ColumnMetadataId",
                        column: x => x.ColumnMetadataId,
                        principalTable: "Senparc_DynamicData_ColumnMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Senparc_DynamicData_TableData_Senparc_DynamicData_TableMetadata_TableMetadataId",
                        column: x => x.TableMetadataId,
                        principalTable: "Senparc_DynamicData_TableMetadata",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_DynamicData_ColumnMetadata_TableMetadataId",
                table: "Senparc_DynamicData_ColumnMetadata",
                column: "TableMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_DynamicData_TableData_ColumnMetadataId",
                table: "Senparc_DynamicData_TableData",
                column: "ColumnMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_DynamicData_TableData_TableMetadataId",
                table: "Senparc_DynamicData_TableData",
                column: "TableMetadataId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_DynamicData_TableData");

            migrationBuilder.DropTable(
                name: "Senparc_DynamicData_ColumnMetadata");

            migrationBuilder.DropTable(
                name: "Senparc_DynamicData_TableMetadata");
        }
    }
}
