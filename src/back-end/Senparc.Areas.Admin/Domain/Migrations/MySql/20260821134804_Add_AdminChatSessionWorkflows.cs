using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.MySql;

public partial class Add_AdminChatSessionWorkflows : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ADMIN_AdminChatSessionWorkflow",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                SessionId = table.Column<int>(type: "int", nullable: false),
                WorkflowId = table.Column<int>(type: "int", nullable: false),
                WorkflowName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                WorkflowDescription = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                AddedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                Flag = table.Column<bool>(type: "tinyint(1)", nullable: false),
                AddTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                LastUpdateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                TenantId = table.Column<int>(type: "int", nullable: false),
                AdminRemark = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Remark = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ADMIN_AdminChatSessionWorkflow", x => x.Id);
                table.ForeignKey(
                    name: "FK_ADMIN_AdminChatSessionWorkflow_ADMIN_AdminChatSession_Sessio~",
                    column: x => x.SessionId,
                    principalTable: "ADMIN_AdminChatSession",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_ADMIN_AdminChatSessionWorkflow_SessionId_WorkflowId",
            table: "ADMIN_AdminChatSessionWorkflow",
            columns: new[] { "SessionId", "WorkflowId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "ADMIN_AdminChatSessionWorkflow");
}
