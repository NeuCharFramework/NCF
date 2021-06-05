using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Senparc.Xncf.CodeBuilder.Migrations.Migrations.SqlServer
{
    public partial class AddSample : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
               name: "Senparc_CodeBuilder_BuilderTable",
               columns: table => new
               {
                   Id = table.Column<int>(nullable: false)
                       .Annotation("SqlServer:Identity", "1, 1"),
                   Flag = table.Column<bool>(nullable: false),
                   AddTime = table.Column<DateTime>(nullable: false),
                   LastUpdateTime = table.Column<DateTime>(nullable: false),
                   AdminRemark = table.Column<string>(maxLength: 300, nullable: true),
                   Remark = table.Column<string>(maxLength: 300, nullable: true),
                   AdditionNote = table.Column<string>(nullable: true),
                   TenantId = table.Column<int>(nullable: false),

                   TableName = table.Column<string>(maxLength: 300, nullable: true),
                   Comment = table.Column<string>(maxLength: 300, nullable: true),
                   DetailTableName = table.Column<string>(maxLength: 300, nullable: true),
                   DetailComment = table.Column<string>(maxLength: 300, nullable: true),
                   ClassName = table.Column<string>(maxLength: 300, nullable: true),
                   Namespace = table.Column<string>(maxLength: 300, nullable: true),
                   ModuleCode = table.Column<string>(maxLength: 300, nullable: true),
                   ModuleName = table.Column<string>(maxLength: 300, nullable: true),
                   Folder = table.Column<string>(maxLength: 300, nullable: true),
                   Options = table.Column<string>(maxLength: 300, nullable: true),
                   TypeId = table.Column<string>(maxLength: 300, nullable: true),
                   TypeName = table.Column<string>(maxLength: 300, nullable: true),
                   CreateTime = table.Column<DateTime>(nullable: false),
                   CreateUserId = table.Column<string>(maxLength: 300, nullable: true),
                   CreateUserName = table.Column<string>(maxLength: 300, nullable: true),
                   UpdateTime = table.Column<DateTime>(nullable: false),
                   UpdateUserId = table.Column<string>(maxLength: 300, nullable: true),
                   UpdateUserName = table.Column<string>(maxLength: 300, nullable: true)
               },
               constraints: table =>
               {
                   table.PrimaryKey("PK_Senparc_CodeBuilder_BuilderTable", x => x.Id);
               });
            migrationBuilder.CreateTable(
               name: "Senparc_CodeBuilder_BuilderTableColumn",
               columns: table => new
               {
                   Id = table.Column<int>(nullable: false)
                       .Annotation("SqlServer:Identity", "1, 1"),
                   Flag = table.Column<bool>(nullable: false),
                   AddTime = table.Column<DateTime>(nullable: false),
                   LastUpdateTime = table.Column<DateTime>(nullable: false),
                   AdminRemark = table.Column<string>(maxLength: 300, nullable: true),
                   Remark = table.Column<string>(maxLength: 300, nullable: true),
                   AdditionNote = table.Column<string>(nullable: true),
                   TenantId = table.Column<int>(nullable: false),
                   TableId = table.Column<int>(nullable: false),
                   TableName = table.Column<string>(maxLength: 300, nullable: true),
                   ColumnName = table.Column<string>(maxLength: 300, nullable: true),
                   Comment = table.Column<string>(maxLength: 300, nullable: true),
                   ColumnType = table.Column<string>(maxLength: 300, nullable: true),
                   EntityType = table.Column<string>(maxLength: 300, nullable: true),
                   EntityName = table.Column<string>(maxLength: 300, nullable: true),
                   IsKey = table.Column<bool>(nullable: false),
                   IsIncrement = table.Column<bool>(nullable: false),
                   IsRequired = table.Column<bool>(nullable: false),
                   IsInsert = table.Column<bool>(nullable: false),
                   IsEdit = table.Column<bool>(nullable: false),
                   IsList = table.Column<bool>(nullable: false),
                   IsQuery = table.Column<bool>(nullable: false),
                   QueryType = table.Column<string>(maxLength: 300, nullable: true),
                   HtmlType = table.Column<string>(maxLength: 300, nullable: true),
                   EditType = table.Column<string>(maxLength: 300, nullable: true),
                   Sort = table.Column<int>(nullable: false),
                   CreateTime = table.Column<DateTime>(nullable: false),
                   CreateUserId = table.Column<string>(maxLength: 300, nullable: true),
                   CreateUserName = table.Column<string>(maxLength: 300, nullable: true),
                   UpdateTime = table.Column<DateTime>(nullable: false),
                   UpdateUserId = table.Column<string>(maxLength: 300, nullable: true),
                   UpdateUserName = table.Column<string>(maxLength: 300, nullable: true),
                   EditRow = table.Column<string>(maxLength: 300, nullable: true),
                   EditCol = table.Column<string>(maxLength: 300, nullable: true),
                   MaxLength = table.Column<int>(nullable: false),
                   DataSource = table.Column<string>(maxLength: 300, nullable: true)
               },
               constraints: table =>
               {
                   table.PrimaryKey("PK_Senparc_CodeBuilder_BuilderTableColumn", x => x.Id);
               });
                migrationBuilder.CreateTable(
                 name: "Senparc_CodeBuilder_UserBuilderLog",
                 columns: table => new
                 {
                     Flag = table.Column<bool>(nullable: false, comment: "是否删除"),
                     AddTime = table.Column<DateTime>(nullable: false, comment: ""),
                     LastUpdateTime = table.Column<DateTime>(nullable: false, comment: "最后更新时间"),
                     AdminRemark = table.Column<string>(nullable: true, comment: ""),
                     Remark = table.Column<string>(nullable: true, comment: "备注"),
                     AdditionNote = table.Column<string>(nullable: true, comment: ""),
                     TenantId = table.Column<int>(nullable: false, comment: "租户Id"),
                     UserId = table.Column<string>(nullable: true, comment: "用户Id"),
                     TableName = table.Column<string>(nullable: true, comment: "表名"),
                     ModuleName = table.Column<string>(nullable: true, comment: "模块名称"),
                     Path = table.Column<string>(nullable: true, comment: "存放路径"),
                     Count = table.Column<int>(nullable: true, comment: "发送次数"),
                     Id = table.Column<int>(nullable: false, comment: "Id").Annotation("SqlServer: Identity", "1, 1")
                 },
                 constraints: table =>
                 {
                     table.PrimaryKey("PK_Senparc_CodeBuilder_UserBuilderLog", x => x.Id);
                 });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_CodeBuilder_BuilderTable");
            migrationBuilder.DropTable(
                name: "Senparc_CodeBuilder_BuilderTableColumn");
        }
    }
}

