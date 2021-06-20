using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Senparc.Service.Migrations.Migrations.MySql
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", 1/*MySqlValueGenerationStrategy.IdentityColumn*/),
                    Flag = table.Column<bool>(nullable: false, defaultValue: false),
                    AddTime = table.Column<DateTime>(nullable: false),
                    LastUpdateTime = table.Column<DateTime>(nullable: false),
                    AdminRemark = table.Column<string>(maxLength: 300, nullable: true),
                    Remark = table.Column<string>(maxLength: 300, nullable: true),
                    UserName = table.Column<string>(maxLength: 50, nullable: false),
                    Password = table.Column<string>(maxLength: 100, nullable: true),
                    PasswordSalt = table.Column<string>(unicode: false, maxLength: 100, nullable: true),
                    NickName = table.Column<string>(maxLength: 50, nullable: false),
                    RealName = table.Column<string>(maxLength: 100, nullable: true),
                    Phone = table.Column<string>(maxLength: 20, nullable: true),
                    PhoneChecked = table.Column<bool>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    EmailChecked = table.Column<bool>(nullable: true),
                    PicUrl = table.Column<string>(unicode: false, maxLength: 300, nullable: true),
                    HeadImgUrl = table.Column<string>(nullable: true),
                    Package = table.Column<decimal>(nullable: false),
                    Balance = table.Column<decimal>(nullable: false),
                    LockMoney = table.Column<decimal>(nullable: false),
                    Sex = table.Column<byte>(nullable: false),
                    QQ = table.Column<string>(nullable: true),
                    Country = table.Column<string>(maxLength: 30, nullable: true),
                    Province = table.Column<string>(maxLength: 20, nullable: true),
                    City = table.Column<string>(maxLength: 30, nullable: true),
                    District = table.Column<string>(nullable: true),
                    Address = table.Column<string>(nullable: true),
                    Note = table.Column<string>(nullable: true),
                    ThisLoginTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    ThisLoginIP = table.Column<string>(unicode: false, maxLength: 30, nullable: true),
                    LastLoginTime = table.Column<DateTime>(nullable: false),
                    LastLoginIP = table.Column<string>(nullable: true),
                    Points = table.Column<decimal>(nullable: false),
                    LastWeixinSignInTime = table.Column<DateTime>(nullable: true),
                    WeixinSignTimes = table.Column<int>(nullable: false),
                    WeixinUnionId = table.Column<string>(nullable: true),
                    WeixinOpenId = table.Column<string>(nullable: true),
                    Locked = table.Column<bool>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdminUserInfos",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", 1/*MySqlValueGenerationStrategy.IdentityColumn*/),
                    Flag = table.Column<bool>(nullable: false),
                    AddTime = table.Column<DateTime>(nullable: false),
                    LastUpdateTime = table.Column<DateTime>(nullable: false),
                    AdminRemark = table.Column<string>(maxLength: 300, nullable: true),
                    Remark = table.Column<string>(maxLength: 300, nullable: true),
                    UserName = table.Column<string>(maxLength: 50, nullable: true),
                    Password = table.Column<string>(maxLength: 50, nullable: true),
                    PasswordSalt = table.Column<string>(unicode: false, maxLength: 100, nullable: true),
                    RealName = table.Column<string>(maxLength: 50, nullable: true),
                    Phone = table.Column<string>(unicode: false, maxLength: 20, nullable: true),
                    Note = table.Column<string>(nullable: true),
                    ThisLoginTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    ThisLoginIP = table.Column<string>(unicode: false, maxLength: 20, nullable: true),
                    LastLoginTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastLoginIP = table.Column<string>(unicode: false, maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUserInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SysButtons",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Flag = table.Column<bool>(nullable: false),
                    AddTime = table.Column<DateTime>(nullable: false),
                    LastUpdateTime = table.Column<DateTime>(nullable: false),
                    AdminRemark = table.Column<string>(maxLength: 300, nullable: true),
                    Remark = table.Column<string>(maxLength: 300, nullable: true),
                    MenuId = table.Column<string>(maxLength: 50, nullable: true),
                    ButtonName = table.Column<string>(maxLength: 50, nullable: false),
                    OpearMark = table.Column<string>(maxLength: 50, nullable: true),
                    Url = table.Column<string>(maxLength: 350, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysButtons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SysMenus",
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 50, nullable: false),
                    Flag = table.Column<bool>(nullable: false),
                    AddTime = table.Column<DateTime>(nullable: false),
                    LastUpdateTime = table.Column<DateTime>(nullable: false),
                    AdminRemark = table.Column<string>(maxLength: 300, nullable: true),
                    Remark = table.Column<string>(maxLength: 300, nullable: true),
                    MenuName = table.Column<string>(maxLength: 150, nullable: false),
                    ParentId = table.Column<string>(maxLength: 50, nullable: true),
                    Url = table.Column<string>(maxLength: 350, nullable: true),
                    Icon = table.Column<string>(maxLength: 50, nullable: true),
                    IsLocked = table.Column<bool>(nullable: false),
                    MenuType = table.Column<int>(nullable: false),
                    ResourceCode = table.Column<string>(maxLength: 30, nullable: true),
                    Sort = table.Column<int>(nullable: false),
                    Visible = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysMenus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SysPermission",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", 1/*MySqlValueGenerationStrategy.IdentityColumn*/),
                    Flag = table.Column<bool>(nullable: false),
                    AddTime = table.Column<DateTime>(nullable: false),
                    LastUpdateTime = table.Column<DateTime>(nullable: false),
                    AdminRemark = table.Column<string>(maxLength: 300, nullable: true),
                    Remark = table.Column<string>(maxLength: 300, nullable: true),
                    RoleCode = table.Column<string>(maxLength: 20, nullable: true),
                    ResourceCode = table.Column<string>(maxLength: 20, nullable: true),
                    RoleId = table.Column<string>(maxLength: 50, nullable: true),
                    IsMenu = table.Column<bool>(nullable: false),
                    PermissionId = table.Column<string>(maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysPermission", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SysRoleAdminUserInfos",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", 1/*MySqlValueGenerationStrategy.IdentityColumn*/),
                    Flag = table.Column<bool>(nullable: false),
                    AddTime = table.Column<DateTime>(nullable: false),
                    LastUpdateTime = table.Column<DateTime>(nullable: false),
                    AdminRemark = table.Column<string>(maxLength: 300, nullable: true),
                    Remark = table.Column<string>(maxLength: 300, nullable: true),
                    RoleCode = table.Column<string>(maxLength: 20, nullable: true),
                    AccountId = table.Column<int>(nullable: false),
                    RoleId = table.Column<string>(maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysRoleAdminUserInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SysRoles",
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 50, nullable: false),
                    Flag = table.Column<bool>(nullable: false),
                    AddTime = table.Column<DateTime>(nullable: false),
                    LastUpdateTime = table.Column<DateTime>(nullable: false),
                    AdminRemark = table.Column<string>(maxLength: 300, nullable: true),
                    Remark = table.Column<string>(maxLength: 300, nullable: true),
                    Enabled = table.Column<bool>(nullable: false),
                    RoleName = table.Column<string>(maxLength: 50, nullable: true),
                    RoleCode = table.Column<string>(maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", 1/*MySqlValueGenerationStrategy.IdentityColumn*/),
                    Flag = table.Column<bool>(nullable: false),
                    AddTime = table.Column<DateTime>(nullable: false),
                    LastUpdateTime = table.Column<DateTime>(nullable: false),
                    AdminRemark = table.Column<string>(maxLength: 300, nullable: true),
                    Remark = table.Column<string>(maxLength: 300, nullable: true),
                    SystemName = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    MchId = table.Column<string>(type: "varchar(100)", nullable: true),
                    MchKey = table.Column<string>(type: "varchar(300)", nullable: true),
                    TenPayAppId = table.Column<string>(type: "varchar(100)", nullable: true),
                    HideModuleManager = table.Column<bool>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "XncfModules",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", 1/*MySqlValueGenerationStrategy.IdentityColumn*/),
                    Flag = table.Column<bool>(nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    AdminRemark = table.Column<string>(maxLength: 300, nullable: true),
                    Remark = table.Column<string>(maxLength: 300, nullable: true),
                    Name = table.Column<string>(maxLength: 100, nullable: false),
                    Uid = table.Column<string>(maxLength: 100, nullable: false),
                    MenuName = table.Column<string>(maxLength: 100, nullable: true),
                    Version = table.Column<string>(maxLength: 100, nullable: false),
                    Description = table.Column<string>(nullable: true),
                    UpdateLog = table.Column<string>(type: "text", nullable: false),
                    AllowRemove = table.Column<bool>(nullable: false),
                    MenuId = table.Column<string>(maxLength: 100, nullable: true),
                    Icon = table.Column<string>(maxLength: 100, nullable: true),
                    State = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XncfModules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountPayLogs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", 1/*MySqlValueGenerationStrategy.IdentityColumn*/),
                    Flag = table.Column<bool>(nullable: false),
                    AddTime = table.Column<DateTime>(nullable: false),
                    LastUpdateTime = table.Column<DateTime>(nullable: false),
                    AdminRemark = table.Column<string>(maxLength: 300, nullable: true),
                    Remark = table.Column<string>(maxLength: 300, nullable: true),
                    AccountId = table.Column<int>(nullable: false),
                    OrderNumber = table.Column<string>(type: "varchar(100)", nullable: false),
                    TotalPrice = table.Column<decimal>(nullable: false),
                    PayMoney = table.Column<decimal>(nullable: false),
                    UsedPoints = table.Column<decimal>(type: "decimal(18, 2)", nullable: true),
                    CompleteTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    AddIp = table.Column<string>(type: "varchar(50)", nullable: true),
                    GetPoints = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Status = table.Column<byte>(nullable: false),
                    Description = table.Column<string>(type: "varchar(250)", nullable: false),
                    Type = table.Column<byte>(nullable: true),
                    TradeNumber = table.Column<string>(type: "varchar(150)", nullable: true),
                    PrepayId = table.Column<string>(type: "varchar(100)", nullable: true),
                    PayType = table.Column<int>(nullable: false),
                    OrderType = table.Column<int>(nullable: false),
                    PayParam = table.Column<string>(nullable: true),
                    Price = table.Column<decimal>(nullable: false),
                    Fee = table.Column<decimal>(type: "decimal(18, 2)", nullable: false)
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
                name: "FeedBacks",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", 1/*MySqlValueGenerationStrategy.IdentityColumn*/),
                    Flag = table.Column<bool>(nullable: false),
                    AddTime = table.Column<DateTime>(nullable: false),
                    LastUpdateTime = table.Column<DateTime>(nullable: false),
                    AdminRemark = table.Column<string>(maxLength: 300, nullable: true),
                    Remark = table.Column<string>(maxLength: 300, nullable: true),
                    AccountId = table.Column<int>(nullable: false),
                    Content = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedBacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedBacks_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PointsLogs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", 1/*MySqlValueGenerationStrategy.IdentityColumn*/),
                    Flag = table.Column<bool>(nullable: false),
                    AddTime = table.Column<DateTime>(nullable: false),
                    LastUpdateTime = table.Column<DateTime>(nullable: false),
                    AdminRemark = table.Column<string>(maxLength: 300, nullable: true),
                    Remark = table.Column<string>(maxLength: 300, nullable: true),
                    AccountId = table.Column<int>(nullable: false),
                    AccountPayLogId = table.Column<int>(nullable: true),
                    Points = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    BeforePoints = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    AfterPoints = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointsLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PointsLogs_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PointsLogs_AccountPayLogs_AccountPayLogId",
                        column: x => x.AccountPayLogId,
                        principalTable: "AccountPayLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountPayLogs_AccountId",
                table: "AccountPayLogs",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedBacks_AccountId",
                table: "FeedBacks",
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
                name: "AdminUserInfos");

            migrationBuilder.DropTable(
                name: "FeedBacks");

            migrationBuilder.DropTable(
                name: "PointsLogs");

            migrationBuilder.DropTable(
                name: "SysButtons");

            migrationBuilder.DropTable(
                name: "SysMenus");

            migrationBuilder.DropTable(
                name: "SysPermission");

            migrationBuilder.DropTable(
                name: "SysRoleAdminUserInfos");

            migrationBuilder.DropTable(
                name: "SysRoles");

            migrationBuilder.DropTable(
                name: "SystemConfigs");

            migrationBuilder.DropTable(
                name: "XncfModules");

            migrationBuilder.DropTable(
                name: "AccountPayLogs");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
