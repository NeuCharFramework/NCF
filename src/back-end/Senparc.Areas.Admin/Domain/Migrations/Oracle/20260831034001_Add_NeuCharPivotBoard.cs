/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260831034001_Add_NeuCharPivotBoard.cs
    文件功能描述：20260831034001_Add_NeuCharPivotBoard 相关功能实现


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
    public partial class Add_NeuCharPivotBoard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ADMIN_NeuCharPivotBoard",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    PageKey = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "NCLOB", nullable: true),
                    Columns = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    BlocksJson = table.Column<string>(type: "NCLOB", nullable: true),
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
                    table.PrimaryKey("PK_ADMIN_NeuCharPivotBoard", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuCharPivotBoard_PageKey",
                table: "ADMIN_NeuCharPivotBoard",
                column: "PageKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ADMIN_NeuCharPivotBoard");
        }
    }
}
