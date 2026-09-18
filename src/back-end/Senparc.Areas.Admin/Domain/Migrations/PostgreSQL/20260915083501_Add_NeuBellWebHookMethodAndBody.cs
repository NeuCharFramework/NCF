/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260915083501_Add_NeuBellWebHookMethodAndBody.cs
    文件功能描述：数据库迁移代码


    创建标识：Senparc - 20260915

    修改标识：Senparc - 20260916
    修改描述：v0.9.0 增强 Admin Chat 取消与推理轨迹，并扩展 NeuBell WebHook 请求能力

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.PostgreSQL
{
    /// <inheritdoc />
    public partial class Add_NeuBellWebHookMethodAndBody : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HttpMethod",
                table: "ADMIN_NeuBellWebHookLog",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyTemplate",
                table: "ADMIN_NeuBellWebHook",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HttpMethod",
                table: "ADMIN_NeuBellWebHook",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "POST");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HttpMethod",
                table: "ADMIN_NeuBellWebHookLog");

            migrationBuilder.DropColumn(
                name: "BodyTemplate",
                table: "ADMIN_NeuBellWebHook");

            migrationBuilder.DropColumn(
                name: "HttpMethod",
                table: "ADMIN_NeuBellWebHook");
        }
    }
}
