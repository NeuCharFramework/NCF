using Microsoft.EntityFrameworkCore.Migrations;

namespace Senparc.Service.Migrations.Migrations.SQLite
{
    public partial class AddTelantId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "XncfModules",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "SystemConfigs",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "SysRoles",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "SysRoleAdminUserInfos",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "SysPermission",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "SysMenus",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "SysButtons",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PointsLogs",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "FeedBacks",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AdminUserInfos",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Accounts",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AccountPayLogs",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "XncfModules");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SystemConfigs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SysRoles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SysRoleAdminUserInfos");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SysPermission");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SysMenus");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SysButtons");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PointsLogs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "FeedBacks");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AdminUserInfos");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AccountPayLogs");
        }
    }
}
