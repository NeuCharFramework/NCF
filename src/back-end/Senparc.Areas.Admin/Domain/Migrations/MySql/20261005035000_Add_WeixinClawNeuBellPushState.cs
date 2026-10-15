using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Senparc.Areas.Admin.Domain.Models;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.MySql;

[DbContext(typeof(AdminSenparcEntities_MySql))]
[Migration("20261005035000_Add_WeixinClawNeuBellPushState")]
public partial class Add_WeixinClawNeuBellPushState : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "NeuBellPushStateJson",
            table: "ADMIN_WeixinClawAdminBinding",
            type: "varchar(2000)",
            maxLength: 2000,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "NeuBellPushStateJson",
            table: "ADMIN_WeixinClawAdminBinding");
    }
}
