/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260906192152_Add_NeuBellWebHook.cs
    文件功能描述：20260906192152_Add_NeuBellWebHook 相关功能实现


    创建标识：Senparc - 20220818

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

----------------------------------------------------------------*/
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class Add_NeuBellWebHook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ADMIN_NeuBellWebHook",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    WebHookUrl = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: false),
                    ProviderFilter = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    Secret = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    NotifyOnAdd = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    NotifyOnRemove = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AdminUserId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_NeuBellWebHook", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuBellWebHook_ProviderFilter",
                table: "ADMIN_NeuBellWebHook",
                column: "ProviderFilter");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ADMIN_NeuBellWebHook");
        }
    }
}
