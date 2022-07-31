using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Senparc.Xncf.WeixinManagerMP.Migrations.Migrations.Sqlite
{
    public partial class AddSample : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_WeixinManagerMP_Color",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Flag = table.Column<bool>(nullable: false),
                    AddTime = table.Column<DateTime>(nullable: false),
                    LastUpdateTime = table.Column<DateTime>(nullable: false),
                    AdminRemark = table.Column<string>(maxLength: 300, nullable: true),
                    Remark = table.Column<string>(maxLength: 300, nullable: true),
                    Red = table.Column<int>(nullable: false),
                    Green = table.Column<int>(nullable: false),
                    Blue = table.Column<int>(nullable: false),
                    AdditionNote = table.Column<string>(nullable: true)
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
        }
    }
}
