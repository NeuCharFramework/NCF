using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SenparcConf.Xncf.WechatBot.Domain.Migrations.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class Add_Conf2023 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "SenparcConf_WechatBot_Color",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "Conf2023",
                table: "SenparcConf_WechatBot_Color",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Conf2023",
                table: "SenparcConf_WechatBot_Color");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "SenparcConf_WechatBot_Color",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");
        }
    }
}
