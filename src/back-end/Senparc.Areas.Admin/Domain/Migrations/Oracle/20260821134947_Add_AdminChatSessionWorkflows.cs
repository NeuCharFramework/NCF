/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260821134947_Add_AdminChatSessionWorkflows.cs
    文件功能描述：20260821134947_Add_AdminChatSessionWorkflows.cs 相关实现


    创建标识：Senparc - 20260821

    修改标识：Senparc - 20260822
    修改描述：v0.6.0 新增管理端 Chat 会话工作流能力

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Oracle;

public partial class Add_AdminChatSessionWorkflows : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ADMIN_AdminChatSessionWorkflow",
            columns: table => new
            {
                Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                    .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                SessionId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                WorkflowId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                WorkflowName = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                WorkflowDescription = table.Column<string>(type: "NVARCHAR2(400)", maxLength: 400, nullable: true),
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
                table.PrimaryKey("PK_ADMIN_AdminChatSessionWorkflow", x => x.Id);
                table.ForeignKey(
                    name: "FK_ADMIN_AdminChatSessionWorkflow_ADMIN_AdminChatSession_SessionId",
                    column: x => x.SessionId,
                    principalTable: "ADMIN_AdminChatSession",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ADMIN_AdminChatSessionWorkflow_SessionId_WorkflowId",
            table: "ADMIN_AdminChatSessionWorkflow",
            columns: new[] { "SessionId", "WorkflowId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "ADMIN_AdminChatSessionWorkflow");
}
