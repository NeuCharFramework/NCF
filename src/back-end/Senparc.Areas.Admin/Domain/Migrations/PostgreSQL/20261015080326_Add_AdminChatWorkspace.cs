/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：20261015080326_Add_AdminChatWorkspace.cs
    文件功能描述：EF Core 数据库迁移
    
    
    创建标识：Senparc - 20261015
    
    修改标识：Senparc - 20261021
    修改描述：v0.11.1 为 AdminChat 会话增加工作区字段迁移

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 支持 Add_AdminChatWorkspace 数据库迁移

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.PostgreSQL
{
    public partial class Add_AdminChatWorkspace : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkspaceId",
                table: "ADMIN_AdminChatSession",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WorkspaceWriteEnabled",
                table: "ADMIN_AdminChatSession",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WorkspaceCliEnabled",
                table: "ADMIN_AdminChatSession",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn("WorkspaceCliEnabled", "ADMIN_AdminChatSession");
            migrationBuilder.DropColumn("WorkspaceWriteEnabled", "ADMIN_AdminChatSession");
            migrationBuilder.DropColumn("WorkspaceId", "ADMIN_AdminChatSession");
        }
    }
}
