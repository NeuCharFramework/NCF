/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261102173543_Add_AdminChat_Session_SortOrder.cs
    文件功能描述：20261102173543_Add_AdminChat_Session_SortOrder 相关定义与实现


    创建标识：Senparc - 20250816

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 支持 Add_AdminChat_Session_SortOrder 数据库迁移

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Add_AdminChat_Session_SortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "ADMIN_AdminChatSession",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "ADMIN_AdminChatSession");
        }
    }
}
