using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Senparc.Xncf.FileServer.Migrations
{
    public partial class InitFileServer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileServer_SysKey",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Flag = table.Column<bool>(nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    AdminRemark = table.Column<string>(maxLength: 300, nullable: true),
                    Remark = table.Column<string>(maxLength: 300, nullable: true),
                    AppId = table.Column<string>(maxLength: 32, nullable: false),
                    AppKey = table.Column<string>(maxLength: 64, nullable: false),
                    Name = table.Column<string>(maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileServer_SysKey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileServer_FileRecord",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Flag = table.Column<bool>(nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    AdminRemark = table.Column<string>(maxLength: 300, nullable: true),
                    Remark = table.Column<string>(maxLength: 300, nullable: true),
                    SysKeyId = table.Column<int>(nullable: false),
                    FileContentType = table.Column<string>(maxLength: 150, nullable: false),
                    FileName = table.Column<string>(maxLength: 150, nullable: false),
                    PrefixPath = table.Column<string>(maxLength: 50, nullable: true),
                    FilePath = table.Column<string>(maxLength: 200, nullable: false),
                    FileHash = table.Column<string>(maxLength: 64, nullable: false),
                    FileSize = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileServer_FileRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileServer_FileRecord_FileServer_SysKey_SysKeyId",
                        column: x => x.SysKeyId,
                        principalTable: "FileServer_SysKey",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileServer_FileRecord_SysKeyId",
                table: "FileServer_FileRecord",
                column: "SysKeyId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileServer_FileRecord");

            migrationBuilder.DropTable(
                name: "FileServer_SysKey");
        }
    }
}
