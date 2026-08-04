/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DatabaseUpgradeCommandLineOptions.cs
    文件功能描述：解析数据库独立升级命令

    创建标识：Senparc - 20260803

    修改标识：Senparc - 20260804
    修改描述：v0.36.0 解析并剥离 --database-upgrade 启动参数

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
