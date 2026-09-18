/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuBellWebHookLogService.cs
    文件功能描述：NeuBell WebHook 请求日志服务：
    记录每一次发往 WebHook（WebAPI）的请求数据与结果

    创建标识：Senparc - 20260910

    修改标识：Senparc - 20260914
    修改描述：v0.7.1 请求日志新增 HttpMethod（实际请求方式）记录

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

    修改标识：Senparc - 20260916
    修改描述：v0.9.0 增强 Admin Chat 取消与推理轨迹，并扩展 NeuBell WebHook 请求能力

----------------------------------------------------------------*/

using Senparc.Areas.Admin.ACL.Repository;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using System;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
/// 纽铃 WebHook 请求日志服务契约（供 Dispatcher 依赖，便于测试替换）
/// </summary>
public interface INeuBellWebHookLogService
{
    /// <summary>
    /// 创建一条“发送中”的日志（请求发出前调用）
    /// </summary>
    Task<NeuBellWebHookLog> CreatePendingAsync(
        string eventKind,
        string httpMethod,
        string webHookUrl,
        string providerId,
        string title,
        string payload,
        int adminUserId);

    /// <summary>
    /// 记录请求结果（请求完成或失败后调用）
    /// </summary>
    Task CompleteAsync(
        int logId,
        bool success,
        string result,
        int? statusCode,
        long elapsedMilliseconds);
}

/// <summary>
/// 纽铃 WebHook 请求日志服务
/// </summary>
public sealed class NeuBellWebHookLogService : BaseClientService<NeuBellWebHookLog>, INeuBellWebHookLogService
{
    public NeuBellWebHookLogService(
        INeuBellWebHookLogRepository repository,
        IServiceProvider serviceProvider)
        : base(repository, serviceProvider)
    {
    }

    public async Task<NeuBellWebHookLog> CreatePendingAsync(
        string eventKind,
        string httpMethod,
        string webHookUrl,
        string providerId,
        string title,
        string payload,
        int adminUserId)
    {
        var log = new NeuBellWebHookLog(
            eventKind,
            httpMethod,
            webHookUrl,
            providerId,
            title,
            payload,
            adminUserId);
        await SaveObjectAsync(log).ConfigureAwait(false);
        return log;
    }

    public async Task CompleteAsync(
        int logId,
        bool success,
        string result,
        int? statusCode,
        long elapsedMilliseconds)
    {
        var log = await GetObjectAsync(z => z.Id == logId).ConfigureAwait(false);
        if (log == null)
        {
            return;
        }
        log.MarkFinished(success, result, statusCode, elapsedMilliseconds);
        await SaveObjectAsync(log).ConfigureAwait(false);
    }
}
