using Microsoft.EntityFrameworkCore.Migrations;

namespace Senparc.Service.SystemEntities.MigrationsForSenparcEntities
{
    public partial class Update_Menu_20200226 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "SysMenus",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "SysMenus");
        }
    }
}
