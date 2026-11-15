/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260911075432_Add_NeuBellWebHookLog.cs
    文件功能描述：20260911075432_Add_NeuBellWebHookLog 相关功能实现


    创建标识：Senparc - 20211211

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

----------------------------------------------------------------*/
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class Add_NeuBellWebHookLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ADMIN_NeuBellWebHookLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventKind = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WebHookUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StatusCode = table.Column<int>(type: "int", nullable: true),
                    ElapsedMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                    FinishTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdminUserId = table.Column<int>(type: "int", nullable: false),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_NeuBellWebHookLog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuBellWebHookLog_AddTime",
                table: "ADMIN_NeuBellWebHookLog",
                column: "AddTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ADMIN_NeuBellWebHookLog");
        }
    }
}
