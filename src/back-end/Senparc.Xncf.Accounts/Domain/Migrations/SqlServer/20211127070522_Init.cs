using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.Accounts.Domain.Migrations.SqlServer
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PasswordSalt = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    NickName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RealName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PhoneChecked = table.Column<bool>(type: "bit", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailChecked = table.Column<bool>(type: "bit", nullable: true),
                    PicUrl = table.Column<string>(type: "varchar(300)", unicode: false, maxLength: 300, nullable: true),
                    HeadImgUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Package = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LockMoney = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Sex = table.Column<byte>(type: "tinyint", nullable: false),
                    QQ = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Province = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    City = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    District = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThisLoginTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ThisLoginIP = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    LastLoginTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginIP = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Points = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastWeixinSignInTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WeixinSignTimes = table.Column<int>(type: "int", nullable: false),
                    WeixinUnionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WeixinOpenId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Locked = table.Column<bool>(type: "bit", nullable: true),
                    Flag = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AddTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountPayLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    OrderNumber = table.Column<string>(type: "varchar(100)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PayMoney = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UsedPoints = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CompleteTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddIp = table.Column<string>(type: "varchar(50)", nullable: true),
                    GetPoints = table.Column<decimal>(type: "money", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Description = table.Column<string>(type: "varchar(250)", nullable: false),
                    Type = table.Column<byte>(type: "tinyint", nullable: true),
                    TradeNumber = table.Column<string>(type: "varchar(150)", nullable: true),
                    PrepayId = table.Column<string>(type: "varchar(100)", nullable: true),
                    PayType = table.Column<int>(type: "int", nullable: false),
                    OrderType = table.Column<int>(type: "int", nullable: false),
                    PayParam = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Fee = table.Column<decimal>(type: "money", nullable: false),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountPayLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountPayLogs_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PointsLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    AccountPayLogId = table.Column<int>(type: "int", nullable: true),
                    Points = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BeforePoints = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AfterPoints = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointsLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PointsLogs_AccountPayLogs_AccountPayLogId",
                        column: x => x.AccountPayLogId,
                        principalTable: "AccountPayLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PointsLogs_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountPayLogs_AccountId",
                table: "AccountPayLogs",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PointsLogs_AccountId",
                table: "PointsLogs",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PointsLogs_AccountPayLogId",
                table: "PointsLogs",
                column: "AccountPayLogId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PointsLogs");

            migrationBuilder.DropTable(
                name: "AccountPayLogs");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
