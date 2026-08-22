/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：20260808150852_Add_NeuCharPivot_Workflow.cs
    文件功能描述：数据库迁移与模型快照


    创建标识：Senparc - 20260809

    修改标识：Senparc - 20260813
    修改描述：v0.5.0 集成 NeuCharPivot 与 NeuCharWorkflow 管理能力并优化后台体验

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Add_NeuCharPivot_Workflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ADMIN_NeuCharExecutionLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceType = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    SourceId = table.Column<int>(type: "INTEGER", nullable: false),
                    ModuleUid = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FunctionKey = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Succeeded = table.Column<bool>(type: "INTEGER", nullable: true),
                    ResultSummary = table.Column<string>(type: "TEXT", nullable: true),
                    Error = table.Column<string>(type: "TEXT", nullable: true),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_NeuCharExecutionLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ADMIN_NeuCharPivotConfiguration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModuleUid = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UserRequirement = table.Column<string>(type: "TEXT", nullable: true),
                    LayoutSchemaJson = table.Column<string>(type: "TEXT", nullable: true),
                    AiModelId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChatSessionId = table.Column<int>(type: "INTEGER", nullable: true),
                    Revision = table.Column<int>(type: "INTEGER", nullable: false),
                    LastGeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastError = table.Column<string>(type: "TEXT", nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_NeuCharPivotConfiguration", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ADMIN_NeuCharPivotFunction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PivotId = table.Column<int>(type: "INTEGER", nullable: false),
                    ModuleUid = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FunctionKey = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FunctionName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    UiSchemaJson = table.Column<string>(type: "TEXT", nullable: true),
                    DefaultParametersJson = table.Column<string>(type: "TEXT", nullable: true),
                    ModuleVersion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Sort = table.Column<int>(type: "INTEGER", nullable: false),
                    Visible = table.Column<bool>(type: "INTEGER", nullable: false),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_NeuCharPivotFunction", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ADMIN_NeuCharPivotLoopTask",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FunctionId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    IntervalSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    ParametersJson = table.Column<string>(type: "TEXT", nullable: true),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    UseNeuBell = table.Column<bool>(type: "INTEGER", nullable: false),
                    NextRunAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastRunAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastSucceeded = table.Column<bool>(type: "INTEGER", nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "INTEGER", nullable: false),
                    LastError = table.Column<string>(type: "TEXT", nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_NeuCharPivotLoopTask", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ADMIN_NeuCharWorkflow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    GraphJson = table.Column<string>(type: "TEXT", nullable: true),
                    AdminUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    TriggerType = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    TriggerConfigJson = table.Column<string>(type: "TEXT", nullable: true),
                    NextRunAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastRunAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastSucceeded = table.Column<bool>(type: "INTEGER", nullable: true),
                    LastError = table.Column<string>(type: "TEXT", nullable: true),
                    Revision = table.Column<int>(type: "INTEGER", nullable: false),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_NeuCharWorkflow", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ADMIN_NeuCharExecutionLog");

            migrationBuilder.DropTable(
                name: "ADMIN_NeuCharPivotConfiguration");

            migrationBuilder.DropTable(
                name: "ADMIN_NeuCharPivotFunction");

            migrationBuilder.DropTable(
                name: "ADMIN_NeuCharPivotLoopTask");

            migrationBuilder.DropTable(
                name: "ADMIN_NeuCharWorkflow");
        }
    }
}
