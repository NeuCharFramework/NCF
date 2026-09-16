using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class Add_NeuBellWebHookMethodAndBody : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HttpMethod",
                table: "ADMIN_NeuBellWebHookLog",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyTemplate",
                table: "ADMIN_NeuBellWebHook",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HttpMethod",
                table: "ADMIN_NeuBellWebHook",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "POST");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HttpMethod",
                table: "ADMIN_NeuBellWebHookLog");

            migrationBuilder.DropColumn(
                name: "BodyTemplate",
                table: "ADMIN_NeuBellWebHook");

            migrationBuilder.DropColumn(
                name: "HttpMethod",
                table: "ADMIN_NeuBellWebHook");
        }
    }
}
