/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：InstallDatabaseState.cs
    文件功能描述：InstallDatabaseState.cs 相关实现


    创建标识：Senparc - 20260728

    修改标识：Senparc - 20260729
    修改描述：v0.4.1 加强安装状态校验并收紧安装辅助路由

----------------------------------------------------------------*/

using System;
using Senparc.Ncf.Core.Utility;

namespace Senparc.Xncf.Installer.Domain.Services
{
    internal static class InstallDatabaseState
    {
        /// <summary>
        /// Determines whether a database exception represents the expected
        /// pre-installation state. The installer can still render its configuration
        /// page in this state; the actual POST must still be able to create/update
        /// the database before installation can complete.
        /// </summary>
        public static bool IsDatabaseUnavailableForInstallation(Exception exception)
            => DatabaseInstallState.IsDatabaseUnavailableForInstallation(exception);
    }
}
