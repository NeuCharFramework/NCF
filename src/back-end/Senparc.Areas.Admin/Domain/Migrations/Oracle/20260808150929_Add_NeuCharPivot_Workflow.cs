/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：20260808150929_Add_NeuCharPivot_Workflow.cs
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
    public partial class Add_NeuCharPivot_Workflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /* Provider-version drift intentionally excluded from this feature migration.
               NeuCharPivot must not rewrite flags in pre-existing tables.
            migrationBuilder.AlterColumn<bool>(
                name: "Flag",
                table: "ADMIN_AdminUserInfos",
                type: "BOOLEAN",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "NUMBER(1)");

            migrationBuilder.AlterColumn<bool>(
                name: "Flag",
                table: "ADMIN_AdminChatSessionModule",
                type: "BOOLEAN",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "NUMBER(1)");

            migrationBuilder.AlterColumn<bool>(
                name: "Flag",
                table: "ADMIN_AdminChatSession",
                type: "BOOLEAN",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "NUMBER(1)");

            migrationBuilder.AlterColumn<bool>(
                name: "Flag",
                table: "ADMIN_AdminChatMessage",
                type: "BOOLEAN",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "NUMBER(1)");

            migrationBuilder.AlterColumn<bool>(
                name: "Flag",
                table: "ADMIN_AdminAuthConfig",
                type: "BOOLEAN",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "NUMBER(1)");
            */

            migrationBuilder.CreateTable(
                name: "ADMIN_NeuCharExecutionLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    SourceType = table.Column<string>(type: "NVARCHAR2(40)", maxLength: 40, nullable: false),
                    SourceId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ModuleUid = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    FunctionKey = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    DisplayName = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    Succeeded = table.Column<bool>(type: "BOOLEAN", nullable: true),
                    ResultSummary = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Error = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    CorrelationId = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    Flag = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_NeuCharExecutionLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ADMIN_NeuCharPivotConfiguration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    ModuleUid = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    UserRequirement = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    LayoutSchemaJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    AiModelId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminUserId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ChatSessionId = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    Revision = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    LastGeneratedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    LastError = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Flag = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_NeuCharPivotConfiguration", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ADMIN_NeuCharPivotFunction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    PivotId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ModuleUid = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    FunctionKey = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    FunctionName = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    UiSchemaJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    DefaultParametersJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    ModuleVersion = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    Sort = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Visible = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    Flag = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_NeuCharPivotFunction", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ADMIN_NeuCharPivotLoopTask",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    FunctionId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminUserId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    IntervalSeconds = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ParametersJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Enabled = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    UseNeuBell = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    NextRunAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    LastRunAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    LastSucceeded = table.Column<bool>(type: "BOOLEAN", nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    LastError = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Flag = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_NeuCharPivotLoopTask", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ADMIN_NeuCharWorkflow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    GraphJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    AdminUserId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Enabled = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    TriggerType = table.Column<string>(type: "NVARCHAR2(40)", maxLength: 40, nullable: true),
                    TriggerConfigJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    NextRunAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    LastRunAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    LastSucceeded = table.Column<bool>(type: "BOOLEAN", nullable: true),
                    LastError = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Revision = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Flag = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
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

            /* Counterpart of the intentionally excluded provider-version drift above.
            migrationBuilder.AlterColumn<bool>(
                name: "Flag",
                table: "ADMIN_AdminUserInfos",
                type: "NUMBER(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "BOOLEAN");

            migrationBuilder.AlterColumn<bool>(
                name: "Flag",
                table: "ADMIN_AdminChatSessionModule",
                type: "NUMBER(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "BOOLEAN");

            migrationBuilder.AlterColumn<bool>(
                name: "Flag",
                table: "ADMIN_AdminChatSession",
                type: "NUMBER(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "BOOLEAN");

            migrationBuilder.AlterColumn<bool>(
                name: "Flag",
                table: "ADMIN_AdminChatMessage",
                type: "NUMBER(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "BOOLEAN");

            migrationBuilder.AlterColumn<bool>(
                name: "Flag",
                table: "ADMIN_AdminAuthConfig",
                type: "NUMBER(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "BOOLEAN");
            */
        }
    }
}
