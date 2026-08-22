using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.PostgreSQL;

public partial class Add_AdminChatSessionWorkflows : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ADMIN_AdminChatSessionWorkflow",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                SessionId = table.Column<int>(type: "integer", nullable: false),
                WorkflowId = table.Column<int>(type: "integer", nullable: false),
                WorkflowName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                WorkflowDescription = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                AddedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                Flag = table.Column<bool>(type: "boolean", nullable: false),
                AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                TenantId = table.Column<int>(type: "integer", nullable: false),
                AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ADMIN_AdminChatSessionWorkflow", x => x.Id);
                table.ForeignKey(
                    name: "FK_ADMIN_AdminChatSessionWorkflow_ADMIN_AdminChatSession_Sessi~",
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
