using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class Update_ChatGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EnterAgentTemplateId",
                table: "Senparc_AgentsManager_ChatGroup",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_ChatGroup_EnterAgentTemplateId",
                table: "Senparc_AgentsManager_ChatGroup",
                column: "EnterAgentTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Senparc_AgentsManager_ChatGroup_Senparc_AgentsManager_AgentTemplate_EnterAgentTemplateId",
                table: "Senparc_AgentsManager_ChatGroup",
                column: "EnterAgentTemplateId",
                principalTable: "Senparc_AgentsManager_AgentTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Senparc_AgentsManager_ChatGroup_Senparc_AgentsManager_AgentTemplate_EnterAgentTemplateId",
                table: "Senparc_AgentsManager_ChatGroup");

            migrationBuilder.DropIndex(
                name: "IX_Senparc_AgentsManager_ChatGroup_EnterAgentTemplateId",
                table: "Senparc_AgentsManager_ChatGroup");

            migrationBuilder.DropColumn(
                name: "EnterAgentTemplateId",
                table: "Senparc_AgentsManager_ChatGroup");
        }
    }
}
