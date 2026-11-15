/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkspaceOwnership.cs
    文件功能描述：按当前管理员判断工作区是否可被接口使用


    创建标识：Senparc - 20261110

    修改标识：Senparc - 20261110
    修改描述：聊天接口只返回并绑定当前账号自己的工作区

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Areas.Admin.Domain.Services;

public static class WorkspaceOwnership
{
    public static IReadOnlyList<WorkspaceDescriptor> SelectOwned(
        IEnumerable<WorkspaceDescriptor> workspaces,
        int ownerId)
    {
        if (ownerId <= 0 || workspaces == null)
        {
            return Array.Empty<WorkspaceDescriptor>();
        }

        return workspaces
            .Where(workspace => IsOwnedBy(workspace, ownerId))
            .ToArray();
    }

    public static bool IsOwnedBy(WorkspaceDescriptor workspace, int ownerId) =>
        workspace != null
        && ownerId > 0
        && !string.IsNullOrWhiteSpace(workspace.Id)
        && workspace.OwnerId == ownerId;
}
