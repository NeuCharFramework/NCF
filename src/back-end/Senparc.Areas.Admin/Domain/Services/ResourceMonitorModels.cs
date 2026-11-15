/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ResourceMonitorModels.cs
    文件功能描述：Admin 首页“资源监控”（24 小时非线性时间轴）的数据模型：
    采样点、时间分档窗口与接口响应。数据仅存于统一的 CO2NET 对象缓存，不写数据库。

    创建标识：Senparc - 20260926
    修改描述：v0.12.0 新增资源监控（后台 10 秒级记录，最多保留 24 小时）

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 Admin home resource monitor - per-Web-process CPU/memory lines + nonlinear 24h time-axis chart

----------------------------------------------------------------*/

using System.Collections.Generic;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    /// 资源监控的单个采样点。所有指标均可选（首帧无差分数值或平台不支持时为 null）。
    /// </summary>
    public sealed class ResourceMonitorPoint
    {
        /// <summary>UTC 时间戳（毫秒）。</summary>
        public long T { get; set; }
        /// <summary>Host CPU 使用率（%）。</summary>
        public double? Cpu { get; set; }
        /// <summary>当前 Web 进程 CPU 使用率（%，归一化到全部核心）。</summary>
        public double? Pcpu { get; set; }
        /// <summary>Host 物理内存使用率（%）。</summary>
        public double? Mem { get; set; }
        /// <summary>当前 Web 进程工作集占物理内存百分比（%）。</summary>
        public double? Pmem { get; set; }
        /// <summary>网络下行（Mbps）。</summary>
        public double? Rx { get; set; }
        /// <summary>网络上行（Mbps）。</summary>
        public double? Tx { get; set; }
    }

    /// <summary>
    /// 一个时间分档（10 秒 / 1 分钟 / 5 分钟）的监控数据窗口。
    /// </summary>
    public sealed class ResourceMonitorWindow
    {
        /// <summary>窗口标识：fine / mid / coarse。</summary>
        public string Key { get; set; } = string.Empty;
        /// <summary>采样分辨率（秒）。</summary>
        public int ResolutionSeconds { get; set; }
        /// <summary>按时间升序排列的采样点。</summary>
        public List<ResourceMonitorPoint> Points { get; set; } = new();
    }

    /// <summary>
    /// GetResourceMonitor 接口响应。
    /// </summary>
    public sealed class ResourceMonitorData
    {
        /// <summary>当前 UTC 时间戳（毫秒）。</summary>
        public long Now { get; set; }
        /// <summary>后台采样间隔（秒）。</summary>
        public int SampleIntervalSeconds { get; set; }
        /// <summary>三个时间分档窗口（fine 10s / mid 1min / coarse 5min）。</summary>
        public List<ResourceMonitorWindow> Windows { get; set; } = new();
    }
}
