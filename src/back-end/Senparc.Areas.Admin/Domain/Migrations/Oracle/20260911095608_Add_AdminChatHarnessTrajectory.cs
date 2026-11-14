/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260911095608_Add_AdminChatHarnessTrajectory.cs
    文件功能描述：20260911095608_Add_AdminChatHarnessTrajectory 相关功能实现


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
    public partial class Add_AdminChatHarnessTrajectory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ADMIN_AdminChatTrajectory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    SessionId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    UserId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ParentTrajectoryId = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    ForkFromSequence = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    Title = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    Mode = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Status = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    LastSequence = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ModelIdentifier = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    SessionStateJson = table.Column<string>(type: "NCLOB", nullable: true),
                    LastError = table.Column<string>(type: "NCLOB", nullable: true),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_AdminChatTrajectory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ADMIN_AdminChatTrajectory_ADMIN_AdminChatSession_SessionId",
                        column: x => x.SessionId,
                        principalTable: "ADMIN_AdminChatSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ADMIN_AdminChatTrajectoryEvent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    TrajectoryId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Sequence = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    EventType = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    Content = table.Column<string>(type: "NCLOB", nullable: true),
                    PayloadJson = table.Column<string>(type: "NCLOB", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    CorrelationId = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    IsReplayable = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_AdminChatTrajectoryEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ADMIN_AdminChatTrajectoryEvent_ADMIN_AdminChatTrajectory_TrajectoryId",
                        column: x => x.TrajectoryId,
                        principalTable: "ADMIN_AdminChatTrajectory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_AdminChatTrajectory_SessionId_UserId_AddTime",
                table: "ADMIN_AdminChatTrajectory",
                columns: new[] { "SessionId", "UserId", "AddTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_AdminChatTrajectoryEvent_TrajectoryId_Sequence",
                table: "ADMIN_AdminChatTrajectoryEvent",
                columns: new[] { "TrajectoryId", "Sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ADMIN_AdminChatTrajectoryEvent");

            migrationBuilder.DropTable(
                name: "ADMIN_AdminChatTrajectory");
        }
    }
}
