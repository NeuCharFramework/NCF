using Microsoft.EntityFrameworkCore.Migrations;

namespace Senparc.Xncf.MaQueKeTang.Migrations.Migrations.MySql
{
    public partial class AddTenantId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Senparc_MaQueKeTang_Color",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Senparc_MaQueKeTang_Color");
        }
    }
}
