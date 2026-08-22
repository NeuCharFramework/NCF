/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：20260808175018_Add_NeuCharWorkflow_AutoSave_Versions.cs
    文件功能描述：数据库迁移与模型快照


    创建标识：Senparc - 20260809

    修改标识：Senparc - 20260813
    修改描述：v0.5.0 集成 NeuCharPivot 与 NeuCharWorkflow 管理能力并优化后台体验

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Oracle
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
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.CreateTable(
                name: "ADMIN_NeuCharWorkflowVersion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    WorkflowId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Revision = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "NCLOB", nullable: true),
                    GraphJson = table.Column<string>(type: "NCLOB", nullable: true),
                    Enabled = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    TriggerType = table.Column<string>(type: "NVARCHAR2(40)", maxLength: 40, nullable: true),
                    TriggerConfigJson = table.Column<string>(type: "NCLOB", nullable: true),
                    AutoSaveMinutes = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminUserId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    SaveSource = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    Flag = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_NeuCharWorkflowVersion", x => x.Id);
                });

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
