/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：20260808151951_Tune_NeuCharPivot_Storage.cs
    文件功能描述：数据库迁移与模型快照


    创建标识：Senparc - 20260809

    修改标识：Senparc - 20260813
    修改描述：v0.5.0 集成 NeuCharPivot 与 NeuCharWorkflow 管理能力并优化后台体验

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Tune_NeuCharPivot_Storage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuCharWorkflow_Enabled_NextRunAt",
                table: "ADMIN_NeuCharWorkflow",
                columns: new[] { "Enabled", "NextRunAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuCharPivotLoopTask_Enabled_NextRunAt",
                table: "ADMIN_NeuCharPivotLoopTask",
                columns: new[] { "Enabled", "NextRunAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuCharPivotLoopTask_FunctionId",
                table: "ADMIN_NeuCharPivotLoopTask",
                column: "FunctionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuCharPivotFunction_ModuleUid",
                table: "ADMIN_NeuCharPivotFunction",
                column: "ModuleUid");

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuCharPivotFunction_PivotId_FunctionKey",
                table: "ADMIN_NeuCharPivotFunction",
                columns: new[] { "PivotId", "FunctionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuCharPivotConfiguration_ModuleUid",
                table: "ADMIN_NeuCharPivotConfiguration",
                column: "ModuleUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuCharExecutionLog_CorrelationId",
                table: "ADMIN_NeuCharExecutionLog",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuCharExecutionLog_SourceType_SourceId",
                table: "ADMIN_NeuCharExecutionLog",
                columns: new[] { "SourceType", "SourceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ADMIN_NeuCharWorkflow_Enabled_NextRunAt",
                table: "ADMIN_NeuCharWorkflow");

            migrationBuilder.DropIndex(
                name: "IX_ADMIN_NeuCharPivotLoopTask_Enabled_NextRunAt",
                table: "ADMIN_NeuCharPivotLoopTask");

            migrationBuilder.DropIndex(
                name: "IX_ADMIN_NeuCharPivotLoopTask_FunctionId",
                table: "ADMIN_NeuCharPivotLoopTask");

            migrationBuilder.DropIndex(
                name: "IX_ADMIN_NeuCharPivotFunction_ModuleUid",
                table: "ADMIN_NeuCharPivotFunction");

            migrationBuilder.DropIndex(
                name: "IX_ADMIN_NeuCharPivotFunction_PivotId_FunctionKey",
                table: "ADMIN_NeuCharPivotFunction");

            migrationBuilder.DropIndex(
                name: "IX_ADMIN_NeuCharPivotConfiguration_ModuleUid",
                table: "ADMIN_NeuCharPivotConfiguration");

            migrationBuilder.DropIndex(
                name: "IX_ADMIN_NeuCharExecutionLog_CorrelationId",
                table: "ADMIN_NeuCharExecutionLog");

            migrationBuilder.DropIndex(
                name: "IX_ADMIN_NeuCharExecutionLog_SourceType_SourceId",
                table: "ADMIN_NeuCharExecutionLog");
        }
    }
}
