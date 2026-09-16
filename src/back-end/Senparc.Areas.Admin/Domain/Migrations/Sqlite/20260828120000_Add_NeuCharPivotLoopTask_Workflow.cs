/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260828120000_Add_NeuCharPivotLoopTask_Workflow.cs
    文件功能描述：数据库迁移定义


    创建标识：Senparc - 20260828

    修改标识：Senparc - 20260829
    修改描述：v0.7.0 新增 NeuCharPivot 全局浮动调用与工作流分析管理能力

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Sqlite;

public partial class Add_NeuCharPivotLoopTask_Workflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "WorkflowId",
            table: "ADMIN_NeuCharPivotLoopTask",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_ADMIN_NeuCharPivotLoopTask_WorkflowId",
            table: "ADMIN_NeuCharPivotLoopTask",
            column: "WorkflowId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ADMIN_NeuCharPivotLoopTask_WorkflowId",
            table: "ADMIN_NeuCharPivotLoopTask");

        migrationBuilder.DropColumn(
            name: "WorkflowId",
            table: "ADMIN_NeuCharPivotLoopTask");
    }
}
