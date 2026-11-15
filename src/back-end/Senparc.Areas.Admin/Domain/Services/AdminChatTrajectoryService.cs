/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatTrajectoryService.cs
    文件功能描述：AdminChatTrajectoryService 相关功能实现


    创建标识：Senparc - 20260915

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

----------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Service;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed class AdminChatTrajectoryService : BaseClientService<AdminChatTrajectory>
{
    private readonly AdminChatTrajectoryEventService _eventService;

    public AdminChatTrajectoryService(
        IAdminChatTrajectoryRepository repository,
        AdminChatTrajectoryEventService eventService,
        IServiceProvider serviceProvider)
        : base(repository, serviceProvider)
    {
        _eventService = eventService;
    }

    public async Task<AdminChatTrajectory> CreateAsync(
        int sessionId,
        int userId,
        string title,
        AdminChatMode mode,
        string modelIdentifier = null,
        int? parentTrajectoryId = null,
        int? forkFromSequence = null)
    {
        var trajectory = new AdminChatTrajectory(
            sessionId,
            userId,
            title,
            mode,
            modelIdentifier,
            parentTrajectoryId,
            forkFromSequence);
        await SaveObjectAsync(trajectory);
        return trajectory;
    }

    public Task<AdminChatTrajectory> GetAsync(int trajectoryId, int userId)
    {
        return GetObjectAsync(z => z.Id == trajectoryId && z.UserId == userId);
    }

    public async Task<IReadOnlyList<AdminChatTrajectory>> GetSessionTrajectoriesAsync(int sessionId, int userId)
    {
        var trajectories = await GetFullListAsync(
            z => z.SessionId == sessionId && z.UserId == userId,
            "AddTime DESC");
        return trajectories.ToList();
    }

    public async Task<AdminChatTrajectoryEvent> AppendAsync(
        AdminChatTrajectory trajectory,
        string eventType,
        string source,
        string name,
        string content,
        string payloadJson,
        string correlationId = null,
        bool isReplayable = true)
    {
        var sequence = trajectory.NextSequence();
        var item = new AdminChatTrajectoryEvent(
            trajectory.Id,
            sequence,
            eventType,
            source,
            name,
            content,
            payloadJson,
            correlationId,
            isReplayable);
        await _eventService.SaveObjectAsync(item);
        trajectory.MarkSequence(sequence);
        await SaveObjectAsync(trajectory);
        return item;
    }

    public async Task<IReadOnlyList<AdminChatTrajectoryEvent>> GetEventsAsync(
        int trajectoryId,
        int userId,
        int? beforeSequence = null,
        string query = null,
        int pageSize = 200)
    {
        var trajectory = await GetAsync(trajectoryId, userId);
        if (trajectory == null)
        {
            return Array.Empty<AdminChatTrajectoryEvent>();
        }

        var events = (await _eventService.GetFullListAsync(
            z => z.TrajectoryId == trajectoryId
                && (!beforeSequence.HasValue || z.Sequence < beforeSequence.Value),
            "Sequence ASC")).AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var keyword = query.Trim();
            events = events.Where(z =>
                    (z.EventType?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (z.Source?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (z.Name?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (z.Content?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (z.PayloadJson?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        return events.Take(Math.Max(1, pageSize)).ToList();
    }

    public async Task<IReadOnlyList<AdminChatTrajectory>> SearchAsync(
        int userId,
        string query,
        int pageSize = 50)
    {
        var trajectories = await GetFullListAsync(z => z.UserId == userId, "AddTime DESC");
        if (string.IsNullOrWhiteSpace(query))
        {
            return trajectories.Take(Math.Max(1, pageSize)).ToList();
        }

        var keyword = query.Trim();
        var matches = new List<AdminChatTrajectory>();
        foreach (var trajectory in trajectories)
        {
            if ((trajectory.Title?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                || (trajectory.LastError?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                matches.Add(trajectory);
                continue;
            }

            var events = (await _eventService.GetFullListAsync(
                z => z.TrajectoryId == trajectory.Id,
                "Sequence DESC")).AsEnumerable();
            if (events.Any(z =>
                    (z.EventType?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (z.Source?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (z.Name?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (z.Content?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (z.PayloadJson?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)))
            {
                matches.Add(trajectory);
            }

            if (matches.Count >= Math.Max(1, pageSize))
            {
                break;
            }
        }

        return matches;
    }

    public async Task UpdateSessionStateAsync(AdminChatTrajectory trajectory, string sessionStateJson)
    {
        trajectory.SetSessionState(sessionStateJson);
        await SaveObjectAsync(trajectory);
    }

    public async Task MarkCompletedAsync(AdminChatTrajectory trajectory)
    {
        trajectory.Complete();
        await SaveObjectAsync(trajectory);
    }

    public async Task MarkWaitingForApprovalAsync(AdminChatTrajectory trajectory)
    {
        trajectory.WaitForApproval();
        await SaveObjectAsync(trajectory);
    }

    public async Task MarkFailedAsync(AdminChatTrajectory trajectory, Exception exception)
    {
        trajectory.Fail(exception?.Message);
        await SaveObjectAsync(trajectory);
    }
}

public sealed class AdminChatTrajectoryEventService : BaseClientService<AdminChatTrajectoryEvent>
{
    public AdminChatTrajectoryEventService(
        IAdminChatTrajectoryEventRepository repository,
        IServiceProvider serviceProvider)
        : base(repository, serviceProvider)
    {
    }
}
