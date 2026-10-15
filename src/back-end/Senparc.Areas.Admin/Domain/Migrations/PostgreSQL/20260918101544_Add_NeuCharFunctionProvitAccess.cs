/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260918101544_Add_NeuCharFunctionProvitAccess.cs
    文件功能描述：数据库迁移与模型快照

    创建标识：Senparc - 20260918
    修改描述：v0.9.1 新增 NeuCharFunctionProvitAccess（Function 全局 Provit 数据库访问策略）迁移

    修改标识：Senparc - 20261005
    修改描述：v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.PostgreSQL
{
    /// <inheritdoc />
    public partial class Add_NeuCharFunctionProvitAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ADMIN_NeuCharFunctionProvitAccess",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ModuleUid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FunctionKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AccessMode = table.Column<int>(type: "integer", nullable: false),
                    AllowedRoleCodes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AllowedPermissionCodes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AllowedUserIds = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_NeuCharFunctionProvitAccess", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_NeuCharFunctionProvitAccess_ModuleUid_FunctionKey",
                table: "ADMIN_NeuCharFunctionProvitAccess",
                columns: new[] { "ModuleUid", "FunctionKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ADMIN_NeuCharFunctionProvitAccess");
        }
    }
}