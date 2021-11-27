using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Senparc.Service.Domain.Migrations.PostgreSQL
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeedBacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedBacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SysButtons",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    MenuId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ButtonName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OpearMark = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Url = table.Column<string>(type: "character varying(350)", maxLength: 350, nullable: true),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysButtons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SysMenus",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MenuName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ParentId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Url = table.Column<string>(type: "character varying(350)", maxLength: 350, nullable: true),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    MenuType = table.Column<int>(type: "integer", nullable: false),
                    ResourceCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Sort = table.Column<int>(type: "integer", nullable: false),
                    Visible = table.Column<bool>(type: "boolean", nullable: false),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysMenus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SysPermission",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ResourceCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RoleId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsMenu = table.Column<bool>(type: "boolean", nullable: false),
                    PermissionId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysPermission", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SysRoleAdminUserInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    RoleId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysRoleAdminUserInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SysRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    RoleName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RoleCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SystemName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MchId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MchKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    TenPayAppId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HideModuleManager = table.Column<bool>(type: "boolean", nullable: true),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Enable = table.Column<bool>(type: "boolean", nullable: false),
                    TenantKey = table.Column<string>(type: "text", nullable: false),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "XncfModules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Uid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MenuName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    UpdateLog = table.Column<string>(type: "text", nullable: false),
                    AllowRemove = table.Column<bool>(type: "boolean", nullable: false),
                    MenuId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Icon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    State = table.Column<int>(type: "integer", nullable: false),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XncfModules", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeedBacks");

            migrationBuilder.DropTable(
                name: "SysButtons");

            migrationBuilder.DropTable(
                name: "SysMenus");

            migrationBuilder.DropTable(
                name: "SysPermission");

            migrationBuilder.DropTable(
                name: "SysRoleAdminUserInfos");

            migrationBuilder.DropTable(
                name: "SysRoles");

            migrationBuilder.DropTable(
                name: "SystemConfigs");

            migrationBuilder.DropTable(
                name: "TenantInfos");

            migrationBuilder.DropTable(
                name: "XncfModules");
        }
    }
}
