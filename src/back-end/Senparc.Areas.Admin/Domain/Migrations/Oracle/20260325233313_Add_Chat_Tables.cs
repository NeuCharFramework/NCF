using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class Add_Chat_Tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ADMIN_AdminChatSession",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Title = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    UserId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Status = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    LastMessageTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_AdminChatSession", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ADMIN_AdminChatMessage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    SessionId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    RoleType = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Content = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Sequence = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    UserFeedback = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ModelIdentifier = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_AdminChatMessage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ADMIN_AdminChatMessage_ADMIN_AdminChatSession_SessionId",
                        column: x => x.SessionId,
                        principalTable: "ADMIN_AdminChatSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ADMIN_AdminChatSessionModule",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    SessionId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    XncfModuleUid = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    ModuleName = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    ModuleVersion = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    AddedTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_AdminChatSessionModule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ADMIN_AdminChatSessionModule_ADMIN_AdminChatSession_SessionId",
                        column: x => x.SessionId,
                        principalTable: "ADMIN_AdminChatSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_AdminChatMessage_SessionId",
                table: "ADMIN_AdminChatMessage",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_AdminChatSessionModule_SessionId",
                table: "ADMIN_AdminChatSessionModule",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ADMIN_AdminChatMessage");

            migrationBuilder.DropTable(
                name: "ADMIN_AdminChatSessionModule");

            migrationBuilder.DropTable(
                name: "ADMIN_AdminChatSession");
        }
    }
}
