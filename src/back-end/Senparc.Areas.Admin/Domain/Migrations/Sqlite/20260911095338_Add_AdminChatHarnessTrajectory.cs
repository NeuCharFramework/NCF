/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260911095338_Add_AdminChatHarnessTrajectory.cs
    文件功能描述：20260911095338_Add_AdminChatHarnessTrajectory 相关功能实现


    创建标识：Senparc - 20211212

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

----------------------------------------------------------------*/
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Sqlite
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentTrajectoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    ForkFromSequence = table.Column<int>(type: "INTEGER", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Mode = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastSequence = table.Column<int>(type: "INTEGER", nullable: false),
                    ModelIdentifier = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SessionStateJson = table.Column<string>(type: "TEXT", nullable: true),
                    LastError = table.Column<string>(type: "TEXT", nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrajectoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Content = table.Column<string>(type: "TEXT", nullable: true),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsReplayable = table.Column<bool>(type: "INTEGER", nullable: false),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
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
