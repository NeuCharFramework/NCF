/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260914174250_Add_AdminChatMessageTrajectory.cs
    文件功能描述：20260914174250_Add_AdminChatMessageTrajectory 相关功能实现


    创建标识：Senparc - 20260915

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

----------------------------------------------------------------*/
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class Add_AdminChatMessageTrajectory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TrajectoryId",
                table: "ADMIN_AdminChatMessage",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrajectorySequence",
                table: "ADMIN_AdminChatMessage",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrajectoryId",
                table: "ADMIN_AdminChatMessage");

            migrationBuilder.DropColumn(
                name: "TrajectorySequence",
                table: "ADMIN_AdminChatMessage");
        }
    }
}
