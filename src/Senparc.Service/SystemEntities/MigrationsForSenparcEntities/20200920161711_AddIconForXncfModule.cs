using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Senparc.Service.SystemEntities.MigrationsForSenparcEntities
{
    public partial class AddIconForXncfModule : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "XncfModules",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastLoginTime",
                table: "AdminUserInfos",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Icon",
                table: "XncfModules");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastLoginTime",
                table: "AdminUserInfos",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime");
        }
    }
}
