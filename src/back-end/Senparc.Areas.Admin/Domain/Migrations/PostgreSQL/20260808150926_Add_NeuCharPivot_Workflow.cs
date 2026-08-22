/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：20260808150926_Add_NeuCharPivot_Workflow.cs
    文件功能描述：数据库迁移与模型快照


    创建标识：Senparc - 20260809

    修改标识：Senparc - 20260813
    修改描述：v0.5.0 集成 NeuCharPivot 与 NeuCharWorkflow 管理能力并优化后台体验

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.PostgreSQL
{
    /// <inheritdoc />
    public partial class Add_NeuCharPivot_Workflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /* Provider-version drift intentionally excluded from this feature migration.
               NeuCharPivot must not rewrite timestamp semantics of pre-existing tables.
            migrationBuilder.AlterColumn<DateTime>(
                name: "ThisLoginTime",
                table: "ADMIN_AdminUserInfos",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateTime",
                table: "ADMIN_AdminUserInfos",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastLoginTime",
                table: "ADMIN_AdminUserInfos",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AddTime",
                table: "ADMIN_AdminUserInfos",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateTime",
                table: "ADMIN_AdminChatSessionModule",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AddedTime",
                table: "ADMIN_AdminChatSessionModule",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AddTime",
                table: "ADMIN_AdminChatSessionModule",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateTime",
                table: "ADMIN_AdminChatSession",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastMessageTime",
                table: "ADMIN_AdminChatSession",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AddTime",
                table: "ADMIN_AdminChatSession",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateTime",
                table: "ADMIN_AdminChatMessage",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AddTime",
                table: "ADMIN_AdminChatMessage",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateTime",
                table: "ADMIN_AdminAuthConfig",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AddTime",
                table: "ADMIN_AdminAuthConfig",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");
            */

            migrationBuilder.CreateTable(
                name: "ADMIN_NeuCharExecutionLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SourceType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SourceId = table.Column<int>(type: "integer", nullable: false),
                    ModuleUid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FunctionKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Succeeded = table.Column<bool>(type: "boolean", nullable: true),
                    ResultSummary = table.Column<string>(type: "text", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_NeuCharExecutionLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ADMIN_NeuCharPivotConfiguration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ModuleUid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UserRequirement = table.Column<string>(type: "text", nullable: true),
                    LayoutSchemaJson = table.Column<string>(type: "text", nullable: true),
                    AiModelId = table.Column<int>(type: "integer", nullable: false),
                    AdminUserId = table.Column<int>(type: "integer", nullable: false),
                    ChatSessionId = table.Column<int>(type: "integer", nullable: true),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    LastGeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_NeuCharPivotConfiguration", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ADMIN_NeuCharPivotFunction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PivotId = table.Column<int>(type: "integer", nullable: false),
                    ModuleUid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FunctionKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FunctionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    UiSchemaJson = table.Column<string>(type: "text", nullable: true),
                    DefaultParametersJson = table.Column<string>(type: "text", nullable: true),
                    ModuleVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Sort = table.Column<int>(type: "integer", nullable: false),
                    Visible = table.Column<bool>(type: "boolean", nullable: false),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_NeuCharPivotFunction", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ADMIN_NeuCharPivotLoopTask",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FunctionId = table.Column<int>(type: "integer", nullable: false),
                    AdminUserId = table.Column<int>(type: "integer", nullable: false),
                    IntervalSeconds = table.Column<int>(type: "integer", nullable: false),
                    ParametersJson = table.Column<string>(type: "text", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    UseNeuBell = table.Column<bool>(type: "boolean", nullable: false),
                    NextRunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastRunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSucceeded = table.Column<bool>(type: "boolean", nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_NeuCharPivotLoopTask", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ADMIN_NeuCharWorkflow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    GraphJson = table.Column<string>(type: "text", nullable: true),
                    AdminUserId = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    TriggerType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    TriggerConfigJson = table.Column<string>(type: "text", nullable: true),
                    NextRunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastRunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSucceeded = table.Column<bool>(type: "boolean", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
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
            migrationBuilder.AlterColumn<DateTime>(
                name: "ThisLoginTime",
                table: "ADMIN_AdminUserInfos",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateTime",
                table: "ADMIN_AdminUserInfos",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastLoginTime",
                table: "ADMIN_AdminUserInfos",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AddTime",
                table: "ADMIN_AdminUserInfos",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateTime",
                table: "ADMIN_AdminChatSessionModule",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AddedTime",
                table: "ADMIN_AdminChatSessionModule",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AddTime",
                table: "ADMIN_AdminChatSessionModule",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateTime",
                table: "ADMIN_AdminChatSession",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastMessageTime",
                table: "ADMIN_AdminChatSession",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AddTime",
                table: "ADMIN_AdminChatSession",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateTime",
                table: "ADMIN_AdminChatMessage",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AddTime",
                table: "ADMIN_AdminChatMessage",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateTime",
                table: "ADMIN_AdminAuthConfig",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AddTime",
                table: "ADMIN_AdminAuthConfig",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            */
        }
    }
}
