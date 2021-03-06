﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Senparc.Service;

namespace Senparc.Service.Migrations.Migrations.MySql
{
    [DbContext(typeof(SystemServiceEntities_MySql))]
    [Migration("20210218043432_TenantInfo_Add_IIgnoreMulitTenant")]
    partial class TenantInfo_Add_IIgnoreMulitTenant
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 64)
                .HasAnnotation("ProductVersion", "5.0.1");

            modelBuilder.Entity("Senparc.Core.Models.AdminUserInfo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<bool>("Flag")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("LastLoginIp")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20) CHARACTER SET utf8mb4")
                        .HasColumnName("LastLoginIP");

                    b.Property<DateTime>("LastLoginTime")
                        .HasColumnType("datetime");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Note")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("Password")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50) CHARACTER SET utf8mb4");

                    b.Property<string>("PasswordSalt")
                        .HasMaxLength(100)
                        .IsUnicode(false)
                        .HasColumnType("varchar(100) CHARACTER SET utf8mb4");

                    b.Property<string>("Phone")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20) CHARACTER SET utf8mb4");

                    b.Property<string>("RealName")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50) CHARACTER SET utf8mb4");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<int>("TenantId")
                        .HasColumnType("int");

                    b.Property<string>("ThisLoginIp")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20) CHARACTER SET utf8mb4")
                        .HasColumnName("ThisLoginIP");

                    b.Property<DateTime>("ThisLoginTime")
                        .HasColumnType("datetime");

                    b.Property<string>("UserName")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50) CHARACTER SET utf8mb4");

                    b.HasKey("Id");

                    b.ToTable("AdminUserInfos");
                });

            modelBuilder.Entity("Senparc.Core.Models.FeedBack", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("AccountId")
                        .HasColumnType("int");

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<string>("Content")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<bool>("Flag")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<int>("TenantId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("AccountId");

                    b.ToTable("FeedBacks");
                });

            modelBuilder.Entity("Senparc.Ncf.Core.Models.Account", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Address")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<decimal>("Balance")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("City")
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30) CHARACTER SET utf8mb4");

                    b.Property<string>("Country")
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30) CHARACTER SET utf8mb4");

                    b.Property<string>("District")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("Email")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<bool?>("EmailChecked")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("Flag")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("tinyint(1)")
                        .HasDefaultValue(false);

                    b.Property<string>("HeadImgUrl")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("LastLoginIP")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<DateTime>("LastLoginTime")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("LastWeixinSignInTime")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("LockMoney")
                        .HasColumnType("decimal(65,30)");

                    b.Property<bool?>("Locked")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("NickName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50) CHARACTER SET utf8mb4");

                    b.Property<string>("Note")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<decimal>("Package")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Password")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100) CHARACTER SET utf8mb4");

                    b.Property<string>("PasswordSalt")
                        .HasMaxLength(100)
                        .IsUnicode(false)
                        .HasColumnType("varchar(100) CHARACTER SET utf8mb4");

                    b.Property<string>("Phone")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20) CHARACTER SET utf8mb4");

                    b.Property<bool?>("PhoneChecked")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("PicUrl")
                        .HasMaxLength(300)
                        .IsUnicode(false)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<decimal>("Points")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Province")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20) CHARACTER SET utf8mb4");

                    b.Property<string>("QQ")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("RealName")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100) CHARACTER SET utf8mb4");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<byte>("Sex")
                        .HasColumnType("tinyint unsigned");

                    b.Property<int>("TenantId")
                        .HasColumnType("int");

                    b.Property<string>("ThisLoginIp")
                        .HasMaxLength(30)
                        .IsUnicode(false)
                        .HasColumnType("varchar(30) CHARACTER SET utf8mb4")
                        .HasColumnName("ThisLoginIP");

                    b.Property<DateTime>("ThisLoginTime")
                        .HasColumnType("datetime");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50) CHARACTER SET utf8mb4");

                    b.Property<string>("WeixinOpenId")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<int>("WeixinSignTimes")
                        .HasColumnType("int");

                    b.Property<string>("WeixinUnionId")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.HasKey("Id");

                    b.ToTable("Accounts");
                });

            modelBuilder.Entity("Senparc.Ncf.Core.Models.AccountPayLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("AccountId")
                        .HasColumnType("int");

                    b.Property<string>("AddIp")
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<DateTime>("CompleteTime")
                        .HasColumnType("datetime");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("varchar(250)");

                    b.Property<decimal>("Fee")
                        .HasColumnType("decimal(18,2)");

                    b.Property<bool>("Flag")
                        .HasColumnType("tinyint(1)");

                    b.Property<decimal>("GetPoints")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("OrderNumber")
                        .IsRequired()
                        .HasColumnType("varchar(100)");

                    b.Property<int>("OrderType")
                        .HasColumnType("int");

                    b.Property<decimal>("PayMoney")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("PayParam")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<int>("PayType")
                        .HasColumnType("int");

                    b.Property<string>("PrepayId")
                        .HasColumnType("varchar(100)");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<byte>("Status")
                        .HasColumnType("tinyint unsigned");

                    b.Property<int>("TenantId")
                        .HasColumnType("int");

                    b.Property<decimal>("TotalPrice")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("TradeNumber")
                        .HasColumnType("varchar(150)");

                    b.Property<byte?>("Type")
                        .HasColumnType("tinyint unsigned");

                    b.Property<decimal?>("UsedPoints")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.HasIndex("AccountId");

                    b.ToTable("AccountPayLogs");
                });

            modelBuilder.Entity("Senparc.Ncf.Core.Models.DataBaseModel.SysButton", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("varchar(255) CHARACTER SET utf8mb4");

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<string>("ButtonName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50) CHARACTER SET utf8mb4");

                    b.Property<bool>("Flag")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("MenuId")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50) CHARACTER SET utf8mb4");

                    b.Property<string>("OpearMark")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50) CHARACTER SET utf8mb4");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<int>("TenantId")
                        .HasColumnType("int");

                    b.Property<string>("Url")
                        .HasMaxLength(350)
                        .HasColumnType("varchar(350) CHARACTER SET utf8mb4");

                    b.HasKey("Id");

                    b.ToTable("SysButtons");
                });

            modelBuilder.Entity("Senparc.Ncf.Core.Models.DataBaseModel.SysMenu", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50) CHARACTER SET utf8mb4");

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<bool>("Flag")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Icon")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50) CHARACTER SET utf8mb4");

                    b.Property<bool>("IsLocked")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("MenuName")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150) CHARACTER SET utf8mb4");

                    b.Property<int>("MenuType")
                        .HasColumnType("int");

                    b.Property<string>("ParentId")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50) CHARACTER SET utf8mb4");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<string>("ResourceCode")
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30) CHARACTER SET utf8mb4");

                    b.Property<int>("Sort")
                        .HasColumnType("int");

                    b.Property<int>("TenantId")
                        .HasColumnType("int");

                    b.Property<string>("Url")
                        .HasMaxLength(350)
                        .HasColumnType("varchar(350) CHARACTER SET utf8mb4");

                    b.Property<bool>("Visible")
                        .HasColumnType("tinyint(1)");

                    b.HasKey("Id");

                    b.ToTable("SysMenus");
                });

            modelBuilder.Entity("Senparc.Ncf.Core.Models.DataBaseModel.SysPermission", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<bool>("Flag")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("IsMenu")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("PermissionId")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50) CHARACTER SET utf8mb4");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<string>("ResourceCode")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20) CHARACTER SET utf8mb4");

                    b.Property<string>("RoleCode")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20) CHARACTER SET utf8mb4");

                    b.Property<string>("RoleId")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50) CHARACTER SET utf8mb4");

                    b.Property<int>("TenantId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("SysPermission");
                });

            modelBuilder.Entity("Senparc.Ncf.Core.Models.DataBaseModel.SysRole", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50) CHARACTER SET utf8mb4");

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<bool>("Enabled")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("Flag")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<string>("RoleCode")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20) CHARACTER SET utf8mb4");

                    b.Property<string>("RoleName")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50) CHARACTER SET utf8mb4");

                    b.Property<int>("TenantId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("SysRoles");
                });

            modelBuilder.Entity("Senparc.Ncf.Core.Models.DataBaseModel.SysRoleAdminUserInfo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("AccountId")
                        .HasColumnType("int");

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<bool>("Flag")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<string>("RoleCode")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20) CHARACTER SET utf8mb4");

                    b.Property<string>("RoleId")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50) CHARACTER SET utf8mb4");

                    b.Property<int>("TenantId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("SysRoleAdminUserInfos");
                });

            modelBuilder.Entity("Senparc.Ncf.Core.Models.DataBaseModel.TenantInfo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<bool>("Enable")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("Flag")
                        .HasColumnType("tinyint(1)");

                    b.Property<Guid>("Guid")
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<string>("TenantKey")
                        .IsRequired()
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.HasKey("Id");

                    b.ToTable("TenantInfos");
                });

            modelBuilder.Entity("Senparc.Ncf.Core.Models.DataBaseModel.XncfModule", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("datetime");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<bool>("AllowRemove")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Description")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<bool>("Flag")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Icon")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100) CHARACTER SET utf8mb4");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("datetime");

                    b.Property<string>("MenuId")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100) CHARACTER SET utf8mb4");

                    b.Property<string>("MenuName")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100) CHARACTER SET utf8mb4");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100) CHARACTER SET utf8mb4");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<int>("State")
                        .HasColumnType("int");

                    b.Property<int>("TenantId")
                        .HasColumnType("int");

                    b.Property<string>("Uid")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100) CHARACTER SET utf8mb4");

                    b.Property<string>("UpdateLog")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Version")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100) CHARACTER SET utf8mb4");

                    b.HasKey("Id");

                    b.ToTable("XncfModules");
                });

            modelBuilder.Entity("Senparc.Ncf.Core.Models.PointsLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("AccountId")
                        .HasColumnType("int");

                    b.Property<int?>("AccountPayLogId")
                        .HasColumnType("int");

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<decimal>("AfterPoints")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("BeforePoints")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("Description")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<bool>("Flag")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("Points")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<int>("TenantId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("AccountId");

                    b.HasIndex("AccountPayLogId");

                    b.ToTable("PointsLogs");
                });

            modelBuilder.Entity("Senparc.Ncf.Core.Models.SystemConfig", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<bool>("Flag")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool?>("HideModuleManager")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("MchId")
                        .HasColumnType("varchar(100)");

                    b.Property<string>("MchKey")
                        .HasColumnType("varchar(300)");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4");

                    b.Property<string>("SystemName")
                        .IsRequired()
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("TenPayAppId")
                        .HasColumnType("varchar(100)");

                    b.Property<int>("TenantId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("SystemConfigs");
                });

            modelBuilder.Entity("Senparc.Core.Models.FeedBack", b =>
                {
                    b.HasOne("Senparc.Ncf.Core.Models.Account", "Account")
                        .WithMany()
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Account");
                });

            modelBuilder.Entity("Senparc.Ncf.Core.Models.AccountPayLog", b =>
                {
                    b.HasOne("Senparc.Ncf.Core.Models.Account", "Account")
                        .WithMany("AccountPayLogs")
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Account");
                });

            modelBuilder.Entity("Senparc.Ncf.Core.Models.PointsLog", b =>
                {
                    b.HasOne("Senparc.Ncf.Core.Models.Account", "Account")
                        .WithMany("PointsLogs")
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Senparc.Ncf.Core.Models.AccountPayLog", "AccountPayLog")
                        .WithMany("PointsLogs")
                        .HasForeignKey("AccountPayLogId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("Account");

                    b.Navigation("AccountPayLog");
                });

            modelBuilder.Entity("Senparc.Ncf.Core.Models.Account", b =>
                {
                    b.Navigation("AccountPayLogs");

                    b.Navigation("PointsLogs");
                });

            modelBuilder.Entity("Senparc.Ncf.Core.Models.AccountPayLog", b =>
                {
                    b.Navigation("PointsLogs");
                });
#pragma warning restore 612, 618
        }
    }
}
