using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Senparc.Service.SystemEntities.MigrationsForSenparcEntities
{
    public partial class Update_SystemConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ThisLoginIP",
                table: "AdminUserInfos",
                newName: "ThisLoginIp");

            migrationBuilder.RenameColumn(
                name: "LastLoginIP",
                table: "AdminUserInfos",
                newName: "LastLoginIp");

            migrationBuilder.AddColumn<bool>(
                name: "HideModuleManager",
                table: "SystemConfigs",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "AdminUserInfos",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ThisLoginTime",
                table: "AdminUserInfos",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            migrationBuilder.AlterColumn<string>(
                name: "ThisLoginIp",
                table: "AdminUserInfos",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RealName",
                table: "AdminUserInfos",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "AdminUserInfos",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordSalt",
                table: "AdminUserInfos",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Password",
                table: "AdminUserInfos",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastLoginTime",
                table: "AdminUserInfos",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            migrationBuilder.AlterColumn<string>(
                name: "LastLoginIp",
                table: "AdminUserInfos",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HideModuleManager",
                table: "SystemConfigs");

            migrationBuilder.RenameColumn(
                name: "ThisLoginIp",
                table: "AdminUserInfos",
                newName: "ThisLoginIP");

            migrationBuilder.RenameColumn(
                name: "LastLoginIp",
                table: "AdminUserInfos",
                newName: "LastLoginIP");

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "AdminUserInfos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ThisLoginTime",
                table: "AdminUserInfos",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime));

            migrationBuilder.AlterColumn<string>(
                name: "ThisLoginIP",
                table: "AdminUserInfos",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RealName",
                table: "AdminUserInfos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "AdminUserInfos",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordSalt",
                table: "AdminUserInfos",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Password",
                table: "AdminUserInfos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastLoginTime",
                table: "AdminUserInfos",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime));

            migrationBuilder.AlterColumn<string>(
                name: "LastLoginIP",
                table: "AdminUserInfos",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);
        }
    }
}
