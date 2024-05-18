using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class Update_ChatGroupMemeber_ChatGroupId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChatGroupId",
                table: "Senparc_AgentsManager_ChatGroupMember",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChatGroupId",
                table: "Senparc_AgentsManager_ChatGroupMember");
        }
    }
}
