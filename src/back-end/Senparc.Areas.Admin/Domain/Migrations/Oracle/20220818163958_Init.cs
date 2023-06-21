using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Oracle
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ADMIN_AdminUserInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    UserName = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Password = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    PasswordSalt = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    RealName = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Phone = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Note = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    ThisLoginTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    ThisLoginIp = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    LastLoginTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastLoginIp = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_AdminUserInfos", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ADMIN_AdminUserInfos");
        }
    }
}
