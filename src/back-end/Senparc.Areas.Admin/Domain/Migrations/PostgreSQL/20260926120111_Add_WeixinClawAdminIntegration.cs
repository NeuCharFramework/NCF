/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260926120111_Add_WeixinClawAdminIntegration.cs
    文件功能描述：20260926120111_Add_WeixinClawAdminIntegration.cs implementation and project behavior.


    创建标识：Senparc - 20260926

    修改标识：Senparc - 20261005
    修改描述：v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.PostgreSQL
{
    /// <inheritdoc />
    public partial class Add_WeixinClawAdminIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ADMIN_WeixinClawAdminBinding",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    FromUserId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    GroupId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    AdminUserId = table.Column<int>(type: "integer", nullable: false),
                    AdminChatSessionId = table.Column<int>(type: "integer", nullable: true),
                    LastTrajectoryId = table.Column<int>(type: "integer", nullable: true),
                    LastTrajectorySequence = table.Column<int>(type: "integer", nullable: false),
                    AiModelId = table.Column<int>(type: "integer", nullable: false),
                    WorkflowId = table.Column<int>(type: "integer", nullable: true),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    EnableNeuBell = table.Column<bool>(type: "boolean", nullable: false),
                    EnableWorkflow = table.Column<bool>(type: "boolean", nullable: false),
                    ContextToken = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_WeixinClawAdminBinding", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_WeixinClawAdminBinding_AccountId_FromUserId_GroupId",
                table: "ADMIN_WeixinClawAdminBinding",
                columns: new[] { "AccountId", "FromUserId", "GroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_WeixinClawAdminBinding_Enabled_EnableNeuBell",
                table: "ADMIN_WeixinClawAdminBinding",
                columns: new[] { "Enabled", "EnableNeuBell" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ADMIN_WeixinClawAdminBinding");
        }
    }
}
