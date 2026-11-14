/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261102152242_Add_AdminChat_Memory_State.cs
    文件功能描述：20261102152242_Add_AdminChat_Memory_State 相关定义与实现


    创建标识：Senparc - 20210108

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 支持 Add_AdminChat_Memory_State 数据库迁移

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Add_AdminChat_Memory_State : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ADMIN_AdminChatMemoryState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Payload = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_AdminChatMemoryState", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_AdminChatMemoryState_TenantId_UserId",
                table: "ADMIN_AdminChatMemoryState",
                columns: new[] { "TenantId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ADMIN_AdminChatMemoryState");
        }
    }
}
