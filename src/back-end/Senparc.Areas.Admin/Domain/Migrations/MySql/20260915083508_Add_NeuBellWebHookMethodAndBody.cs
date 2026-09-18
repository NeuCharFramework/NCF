/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260915083508_Add_NeuBellWebHookMethodAndBody.cs
    文件功能描述：数据库迁移与模型快照

    创建标识：Senparc - 20260915
    修改描述：v0.7.1 WebHook 请求方式（GET/POST/PUT）与请求体模板迁移（MySql 手工编写，因无可用数据库实例）

    修改标识：Senparc - 20260916
    修改描述：v0.9.0 增强 Admin Chat 取消与推理轨迹，并扩展 NeuBell WebHook 请求能力

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.MySql
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
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BodyTemplate",
                table: "ADMIN_NeuBellWebHook",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "HttpMethod",
                table: "ADMIN_NeuBellWebHook",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "POST")
                .Annotation("MySql:CharSet", "utf8mb4");
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
