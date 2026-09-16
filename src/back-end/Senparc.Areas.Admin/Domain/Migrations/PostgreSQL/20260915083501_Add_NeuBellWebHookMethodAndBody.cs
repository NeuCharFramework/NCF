using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.PostgreSQL
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
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyTemplate",
                table: "ADMIN_NeuBellWebHook",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HttpMethod",
                table: "ADMIN_NeuBellWebHook",
                type: "character varying(10)",
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
