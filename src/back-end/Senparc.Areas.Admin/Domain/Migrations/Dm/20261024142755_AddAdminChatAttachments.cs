/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261024142755_AddAdminChatAttachments.cs
    文件功能描述：20261024142755_AddAdminChatAttachments 相关定义与实现


    创建标识：Senparc - 20250120

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 支持 AddAdminChatAttachments 数据库迁移

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class AddAdminChatAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentsJson",
                table: "ADMIN_AdminChatMessage",
                type: "NVARCHAR2(32767)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentsJson",
                table: "ADMIN_AdminChatMessage");
        }
    }
}
