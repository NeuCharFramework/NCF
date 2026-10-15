using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Senparc.Areas.Admin.Domain.Models;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.SqlServer;

[DbContext(typeof(AdminSenparcEntities_SqlServer))]
[Migration("20261005035000_Add_WeixinClawNeuBellPushState")]
public partial class Add_WeixinClawNeuBellPushState : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "NeuBellPushStateJson",
            table: "ADMIN_WeixinClawAdminBinding",
            type: "nvarchar(2000)",
            maxLength: 2000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "NeuBellPushStateJson",
            table: "ADMIN_WeixinClawAdminBinding");
    }
}
