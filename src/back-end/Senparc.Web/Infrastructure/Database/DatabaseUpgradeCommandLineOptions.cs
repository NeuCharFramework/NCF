/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DatabaseUpgradeCommandLineOptions.cs
    文件功能描述：解析数据库独立升级命令

    创建标识：Senparc - 20260803

    修改标识：Senparc - 20260804
    修改描述：v0.35.0 新增数据库升级维护流程与多平台下载入口

----------------------------------------------------------------*/

namespace Senparc.Web.Infrastructure.Database;

public sealed record DatabaseUpgradeCommandLineOptions(bool Enabled, string[] HostArguments)
{
    public const string SwitchName = "--database-upgrade";

    public static DatabaseUpgradeCommandLineOptions Parse(IEnumerable<string> arguments)
    {
        var enabled = false;
        var hostArguments = new List<string>();

        foreach (var argument in arguments ?? Array.Empty<string>())
        {
            if (string.Equals(argument, SwitchName, StringComparison.OrdinalIgnoreCase))
            {
                enabled = true;
                continue;
            }

            hostArguments.Add(argument);
        }

        return new DatabaseUpgradeCommandLineOptions(enabled, hostArguments.ToArray());
    }
}
