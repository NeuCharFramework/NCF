using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Senparc.Xncf.WeixinManagerMP.Migrations.Migrations.Sqlite
{
    public partial class AddTenantId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SenparcDemo_NewApp_Color");

            migrationBuilder.CreateTable(
                name: "Senparc_WeixinManagerMP_Color",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Red = table.Column<int>(type: "INTEGER", nullable: false),
                    Green = table.Column<int>(type: "INTEGER", nullable: false),
                    Blue = table.Column<int>(type: "INTEGER", nullable: false),
                    AdditionNote = table.Column<string>(type: "TEXT", nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_WeixinManagerMP_Color", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_WeixinManagerMP_Color");

            migrationBuilder.CreateTable(
                name: "SenparcDemo_NewApp_Color",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AdditionNote = table.Column<string>(type: "TEXT", nullable: true),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Blue = table.Column<int>(type: "INTEGER", nullable: false),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    Green = table.Column<int>(type: "INTEGER", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Red = table.Column<int>(type: "INTEGER", nullable: false),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SenparcDemo_NewApp_Color", x => x.Id);
                });
        }
    }
}
