using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Senparc.Web.Migrations
{
    public partial class updatemigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResourceCode",
                table: "SysMenus",
                maxLength: 30,
                nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "MenuType",
                table: "SysMenus",
                maxLength: 666,
                nullable: false, defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResourceCode",
                table: "SysMenus");
            migrationBuilder.DropColumn(
                name: "MenuType",
                table: "SysMenus");
        }
    }
}
