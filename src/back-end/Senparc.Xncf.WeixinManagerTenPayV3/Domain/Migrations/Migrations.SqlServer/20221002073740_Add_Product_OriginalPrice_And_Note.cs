using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.WeixinManagerTenPayV3.Domain.Migrations.Migrations.SqlServer
{
    public partial class Add_Product_OriginalPrice_And_Note : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Senparc_WeixinManagerTenPayV3_Product",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalPrice",
                table: "Senparc_WeixinManagerTenPayV3_Product",
                type: "decimal(18,2)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                table: "Senparc_WeixinManagerTenPayV3_Product");

            migrationBuilder.DropColumn(
                name: "OriginalPrice",
                table: "Senparc_WeixinManagerTenPayV3_Product");
        }
    }
}
