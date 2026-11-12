/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：20261106070548_Add_AdminChat_PromptReferences.cs
    文件功能描述：Oracle 迁移，为 AdminChat 消息增加提示词引用字段
    
    
    创建标识：Senparc - 20250120
    
    修改标识：Senparc - 20261110
    修改描述：v0.12.27 将 PromptReferencesJson 列类型改为 NCLOB，避免长文本被截断

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class Add_AdminChat_PromptReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PromptReferencesJson",
                table: "ADMIN_AdminChatMessage",
                type: "NCLOB",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PromptReferencesJson",
                table: "ADMIN_AdminChatMessage");
        }
    }
}
