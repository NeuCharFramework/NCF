/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260926120916_Add_WeixinClawAdminApprovalState.cs
    文件功能描述：20260926120916_Add_WeixinClawAdminApprovalState.cs implementation and project behavior.


    创建标识：Senparc - 20260926

    修改标识：Senparc - 20261005
    修改描述：v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class Add_WeixinClawAdminApprovalState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PendingApprovalArgumentsJson",
                table: "ADMIN_WeixinClawAdminBinding",
                type: "NVARCHAR2(32767)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingApprovalRequestId",
                table: "ADMIN_WeixinClawAdminBinding",
                type: "NVARCHAR2(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingApprovalToolCallId",
                table: "ADMIN_WeixinClawAdminBinding",
                type: "NVARCHAR2(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingApprovalToolName",
                table: "ADMIN_WeixinClawAdminBinding",
                type: "NVARCHAR2(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingApprovalArgumentsJson",
                table: "ADMIN_WeixinClawAdminBinding");

            migrationBuilder.DropColumn(
                name: "PendingApprovalRequestId",
                table: "ADMIN_WeixinClawAdminBinding");

            migrationBuilder.DropColumn(
                name: "PendingApprovalToolCallId",
                table: "ADMIN_WeixinClawAdminBinding");

            migrationBuilder.DropColumn(
                name: "PendingApprovalToolName",
                table: "ADMIN_WeixinClawAdminBinding");
        }
    }
}
