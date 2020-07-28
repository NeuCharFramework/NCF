using Microsoft.EntityFrameworkCore.Migrations;

namespace Senparc.Xncf.ExtensionAreaTemplate.Migrations
{
    public partial class Xncf_AreaTemplate_Init2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdditionNote",
                table: "AreaTemplate_Color",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionNote",
                table: "AreaTemplate_Color");
        }
    }
}
