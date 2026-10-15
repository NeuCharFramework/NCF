/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260926120032_Add_WeixinClawAdminIntegration.cs
    文件功能描述：20260926120032_Add_WeixinClawAdminIntegration.cs implementation and project behavior.


    创建标识：Senparc - 20260926

    修改标识：Senparc - 20261005
    修改描述：v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.SqlServer
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    FromUserId = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    GroupId = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AdminUserId = table.Column<int>(type: "int", nullable: false),
                    AdminChatSessionId = table.Column<int>(type: "int", nullable: true),
                    LastTrajectoryId = table.Column<int>(type: "int", nullable: true),
                    LastTrajectorySequence = table.Column<int>(type: "int", nullable: false),
                    AiModelId = table.Column<int>(type: "int", nullable: false),
                    WorkflowId = table.Column<int>(type: "int", nullable: true),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    EnableNeuBell = table.Column<bool>(type: "bit", nullable: false),
                    EnableWorkflow = table.Column<bool>(type: "bit", nullable: false),
                    ContextToken = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    LastMessageAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_WeixinClawAdminBinding", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_WeixinClawAdminBinding_AccountId_FromUserId_GroupId",
                table: "ADMIN_WeixinClawAdminBinding",
                columns: new[] { "AccountId", "FromUserId", "GroupId" },
                unique: true,
                filter: "[GroupId] IS NOT NULL");

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
