/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：20260808175009_Add_NeuCharWorkflow_AutoSave_Versions.cs
    文件功能描述：数据库迁移与模型快照


    创建标识：Senparc - 20211211

    修改标识：Senparc - 20260813
    修改描述：v0.5.0 集成 NeuCharPivot 与 NeuCharWorkflow 管理能力并优化后台体验

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.MySql
{
    /// <inheritdoc />
    public partial class Add_NeuCharWorkflow_AutoSave_Versions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AutoSaveMinutes",
                table: "ADMIN_NeuCharWorkflow",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.CreateTable(
                name: "ADMIN_NeuCharWorkflowVersion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    WorkflowId = table.Column<int>(type: "int", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GraphJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TriggerType = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TriggerConfigJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AutoSaveMinutes = table.Column<int>(type: "int", nullable: false),
                    AdminUserId = table.Column<int>(type: "int", nullable: false),
                    SaveSource = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Flag = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remark = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_NeuCharWorkflowVersion", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuCharWorkflowVersion_WorkflowId_Revision",
                table: "ADMIN_NeuCharWorkflowVersion",
                columns: new[] { "WorkflowId", "Revision" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ADMIN_NeuCharWorkflowVersion");

            migrationBuilder.DropColumn(
                name: "AutoSaveMinutes",
                table: "ADMIN_NeuCharWorkflow");
        }
    }
}
