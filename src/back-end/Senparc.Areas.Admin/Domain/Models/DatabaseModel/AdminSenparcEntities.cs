/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AdminSenparcEntities.cs
    文件功能描述：AdminSenparcEntities 相关功能实现


    创建标识：Senparc - 20241028

    修改标识：Senparc - 20260705
    修改描述：v0.0.3 新增登录超时配置并补齐多数据库迁移支持

    修改标识：Senparc - 20260705
    修改描述：v0.0.4 新增登录超时配置并补齐多数据库迁移支持

    修改标识：Senparc - 20260809
    修改描述：Workflow 已迁出至 Senparc.Xncf.NeuCharWorkflow，本上下文仅保留 NeuCharPivot 与执行日志

    修改标识：Senparc - 20260813
    修改描述：v0.5.0 集成 NeuCharPivot 与 NeuCharWorkflow 管理能力并优化后台体验

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Database;
using System;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;

namespace Senparc.Areas.Admin.Domain.Models
{
    /// <summary>
    /// 当前 Entities 只为帮助 SenparcEntities 生成 Migration 信息而存在，没有特别的操作意义。
    /// </summary>
    public class AdminSenparcEntities : XncfDatabaseDbContext
    {
        public AdminSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        #region 系统表（无特殊情况不要修改）

        /// <summary>
        /// 系统设置
        /// </summary>
        public DbSet<AdminUserInfo> SystemConfigs { get; set; }

        /// <summary>
        /// 管理后台认证配置
        /// </summary>
        public DbSet<AdminAuthConfig> AdminAuthConfigs { get; set; }

        /// <summary>
        /// 管理后台聊天会话
        /// </summary>
        public DbSet<AdminChatSession> AdminChatSessions { get; set; }

        /// <summary>
        /// 管理后台聊天消息
        /// </summary>
        public DbSet<AdminChatMessage> AdminChatMessages { get; set; }

        /// <summary>
        /// 管理后台聊天会话-模块关联
        /// </summary>
        public DbSet<AdminChatSessionModule> AdminChatSessionModules { get; set; }

        /// <summary>
        /// 管理后台聊天会话-Workflow 关联
        /// </summary>
        public DbSet<AdminChatSessionWorkflow> AdminChatSessionWorkflows { get; set; }

        /// <summary>
        /// 系统级 NeuCharPivot 配置
        /// </summary>
        public DbSet<NeuCharPivotConfiguration> NeuCharPivotConfigurations { get; set; }

        /// <summary>
        /// 可复用的单 Function UI 块
        /// </summary>
        public DbSet<NeuCharPivotFunction> NeuCharPivotFunctions { get; set; }

        /// <summary>
        /// Function 定时任务
        /// </summary>
        public DbSet<NeuCharPivotLoopTask> NeuCharPivotLoopTasks { get; set; }

        /// <summary>
        /// Pivot 与 Loop Task 的统一执行记录
        /// </summary>
        public DbSet<NeuCharExecutionLog> NeuCharExecutionLogs { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point

        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<NeuCharPivotConfiguration>()
                .HasIndex(z => z.ModuleUid)
                .IsUnique();
            modelBuilder.Entity<NeuCharPivotFunction>()
                .HasIndex(z => new { z.PivotId, z.FunctionKey })
                .IsUnique();
            modelBuilder.Entity<NeuCharPivotFunction>()
                .HasIndex(z => z.ModuleUid);
            modelBuilder.Entity<NeuCharPivotLoopTask>()
                .HasIndex(z => z.FunctionId)
                .IsUnique();
            modelBuilder.Entity<NeuCharPivotLoopTask>()
                .HasIndex(z => new { z.Enabled, z.NextRunAt });
            modelBuilder.Entity<NeuCharExecutionLog>()
                .HasIndex(z => z.CorrelationId);
            modelBuilder.Entity<NeuCharExecutionLog>()
                .HasIndex(z => new { z.SourceType, z.SourceId });
            modelBuilder.Entity<AdminChatSessionWorkflow>()
                .HasIndex(z => new { z.SessionId, z.WorkflowId })
                .IsUnique();

            var providerName = Database.ProviderName ?? string.Empty;
            var largeTextType = providerName.Contains("Oracle", StringComparison.OrdinalIgnoreCase)
                ? "NCLOB"
                : providerName.Contains("Dm", StringComparison.OrdinalIgnoreCase)
                    ? "NVARCHAR2(32767)"
                    : providerName.Contains("MySql", StringComparison.OrdinalIgnoreCase)
                        ? "longtext"
                        : providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase)
                            ? "nvarchar(max)"
                            : "TEXT";

            SetLargeText<NeuCharPivotConfiguration>(modelBuilder, largeTextType,
                nameof(NeuCharPivotConfiguration.UserRequirement),
                nameof(NeuCharPivotConfiguration.LayoutSchemaJson),
                nameof(NeuCharPivotConfiguration.LastError));
            SetLargeText<NeuCharPivotFunction>(modelBuilder, largeTextType,
                nameof(NeuCharPivotFunction.Description),
                nameof(NeuCharPivotFunction.UiSchemaJson),
                nameof(NeuCharPivotFunction.DefaultParametersJson));
            SetLargeText<NeuCharPivotLoopTask>(modelBuilder, largeTextType,
                nameof(NeuCharPivotLoopTask.ParametersJson),
                nameof(NeuCharPivotLoopTask.LastError));
            SetLargeText<NeuCharExecutionLog>(modelBuilder, largeTextType,
                nameof(NeuCharExecutionLog.ResultSummary),
                nameof(NeuCharExecutionLog.Error));
        }

        private static void SetLargeText<TEntity>(
            ModelBuilder modelBuilder,
            string columnType,
            params string[] propertyNames)
            where TEntity : class
        {
            var entity = modelBuilder.Entity<TEntity>();
            foreach (var propertyName in propertyNames)
            {
                entity.Property<string>(propertyName).HasColumnType(columnType);
            }
        }
    }
}
