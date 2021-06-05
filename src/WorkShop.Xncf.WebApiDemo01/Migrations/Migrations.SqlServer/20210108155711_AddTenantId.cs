using Microsoft.EntityFrameworkCore.Migrations;

namespace WorkShop.Xncf.WebApiDemo01.Migrations.Migrations.SqlServer
{
    public partial class AddTenantId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "WorkShop_WebApiDemo01_Color",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WorkShop_WebApiDemo01_Color");
        }
    }
}
