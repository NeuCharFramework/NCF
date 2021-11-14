using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Senparc.Service.Migrations.Migrations.SQLite
{
    public partial class Add_TenantInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PasswordSalt = table.Column<string>(type: "TEXT", unicode: false, maxLength: 100, nullable: true),
                    NickName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RealName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    PhoneChecked = table.Column<bool>(type: "INTEGER", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    EmailChecked = table.Column<bool>(type: "INTEGER", nullable: true),
                    PicUrl = table.Column<string>(type: "TEXT", unicode: false, maxLength: 300, nullable: true),
                    HeadImgUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Package = table.Column<decimal>(type: "TEXT", nullable: false),
                    Balance = table.Column<decimal>(type: "TEXT", nullable: false),
                    LockMoney = table.Column<decimal>(type: "TEXT", nullable: false),
                    Sex = table.Column<byte>(type: "INTEGER", nullable: false),
                    QQ = table.Column<string>(type: "TEXT", nullable: true),
                    Country = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Province = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    District = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    ThisLoginTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    ThisLoginIP = table.Column<string>(type: "TEXT", unicode: false, maxLength: 30, nullable: true),
                    LastLoginTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLoginIP = table.Column<string>(type: "TEXT", nullable: true),
                    Points = table.Column<decimal>(type: "TEXT", nullable: false),
                    LastWeixinSignInTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WeixinSignTimes = table.Column<int>(type: "INTEGER", nullable: false),
                    WeixinUnionId = table.Column<string>(type: "TEXT", nullable: true),
                    WeixinOpenId = table.Column<string>(type: "TEXT", nullable: true),
                    Locked = table.Column<bool>(type: "INTEGER", nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdminUserInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Password = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PasswordSalt = table.Column<string>(type: "TEXT", unicode: false, maxLength: 100, nullable: true),
                    RealName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", unicode: false, maxLength: 20, nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    ThisLoginTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    ThisLoginIP = table.Column<string>(type: "TEXT", unicode: false, maxLength: 20, nullable: true),
                    LastLoginTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastLoginIP = table.Column<string>(type: "TEXT", unicode: false, maxLength: 20, nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUserInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SysButtons",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    MenuId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ButtonName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OpearMark = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Url = table.Column<string>(type: "TEXT", maxLength: 350, nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysButtons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SysMenus",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MenuName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    ParentId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Url = table.Column<string>(type: "TEXT", maxLength: 350, nullable: true),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    MenuType = table.Column<int>(type: "INTEGER", nullable: false),
                    ResourceCode = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Sort = table.Column<int>(type: "INTEGER", nullable: false),
                    Visible = table.Column<bool>(type: "INTEGER", nullable: false),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysMenus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SysPermission",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    ResourceCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    RoleId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IsMenu = table.Column<bool>(type: "INTEGER", nullable: false),
                    PermissionId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysPermission", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SysRoleAdminUserInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    AccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysRoleAdminUserInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SysRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    RoleName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    RoleCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SystemName = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    MchId = table.Column<string>(type: "varchar(100)", nullable: true),
                    MchKey = table.Column<string>(type: "varchar(300)", nullable: true),
                    TenPayAppId = table.Column<string>(type: "varchar(100)", nullable: true),
                    HideModuleManager = table.Column<bool>(type: "INTEGER", nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Guid = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Enable = table.Column<bool>(type: "INTEGER", nullable: false),
                    TenantKey = table.Column<string>(type: "TEXT", nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "XncfModules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Uid = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MenuName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Version = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    UpdateLog = table.Column<string>(type: "ntext", nullable: false),
                    AllowRemove = table.Column<bool>(type: "INTEGER", nullable: false),
                    MenuId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XncfModules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountPayLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderNumber = table.Column<string>(type: "varchar(100)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    PayMoney = table.Column<decimal>(type: "TEXT", nullable: false),
                    UsedPoints = table.Column<decimal>(type: "decimal(18, 2)", nullable: true),
                    CompleteTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    AddIp = table.Column<string>(type: "varchar(50)", nullable: true),
                    GetPoints = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Status = table.Column<byte>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "varchar(250)", nullable: false),
                    Type = table.Column<byte>(type: "INTEGER", nullable: true),
                    TradeNumber = table.Column<string>(type: "varchar(150)", nullable: true),
                    PrepayId = table.Column<string>(type: "varchar(100)", nullable: true),
                    PayType = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderType = table.Column<int>(type: "INTEGER", nullable: false),
                    PayParam = table.Column<string>(type: "TEXT", nullable: true),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    Fee = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    AccountPayLogId = table.Column<int>(type: "INTEGER", nullable: true),
                    Points = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    BeforePoints = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    AfterPoints = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
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
                name: "TenantInfos");

            migrationBuilder.DropTable(
                name: "XncfModules");

            migrationBuilder.DropTable(
                name: "AccountPayLogs");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
