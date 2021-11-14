using Microsoft.EntityFrameworkCore.Migrations;

namespace Senparc.Service.Migrations.Migrations.MySql
{
    public partial class TenantInfo_Add_IIgnoreMulitTenant : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "TenantInfos");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "TenantInfos",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
