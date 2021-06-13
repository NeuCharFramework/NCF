using Microsoft.EntityFrameworkCore.Migrations;

namespace Senparc.Xncf.CodeBuilder.Migrations.Migrations.SqlServer
{
    public partial class AddTenantId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<int>(
            //    name: "TenantId",
            //    table: "Senparc_CodeBuilder_Color",
            //    type: "int",
            //    nullable: false,
            //    defaultValue: 0);
            //migrationBuilder.AddColumn<int>(
            //  name: "TenantId",
            //  table: "Senparc_CodeBuilder_CourseCategory",
            //  type: "int",
            //  nullable: false,
            //  defaultValue: 0);
            //migrationBuilder.AddColumn<int>(
            // name: "TenantId",
            // table: "Senparc_CodeBuilder_BuilderTable",
            // type: "int",
            // nullable: false,
            // defaultValue: 0);
            //migrationBuilder.AddColumn<int>(
            // name: "TenantId",
            // table: "Senparc_CodeBuilder_BuilderTableColumn",
            // type: "int",
            // nullable: false,
            // defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropColumn(
            //    name: "TenantId",
            //    table: "Senparc_CodeBuilder_Color");
            //migrationBuilder.DropColumn(
            //    name: "TenantId",
            //    table: "Senparc_CodeBuilder_CourseCategory");
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Senparc_CodeBuilder_BuilderTable");
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Senparc_CodeBuilder_BuilderTableColumn");
        }
    }
}
