/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：HostMetricsCollector.cs
    文件功能描述：跨平台采集当前 Host 的实时 CPU、内存、网络及进程指标

    创建标识：Senparc - 20260806

    修改标识：Senparc - 20260808
    修改描述：v0.4.0 新增 Host CPU/内存/网络实时指标采集

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    /// 当前 Host 的一次实时指标快照。该模型不包含历史数据，也不会写入缓存。
    /// </summary>
    public sealed class HostMetricsSnapshot
    {
        public DateTimeOffset SampledAt { get; set; }
        public string HostName { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public double? CpuUsagePercent { get; set; }
        public long MemoryTotalBytes { get; set; }
        public long MemoryUsedBytes { get; set; }
        public long MemoryAvailableBytes { get; set; }
        public double? MemoryUsagePercent { get; set; }
        public double? NetworkReceiveBytesPerSecond { get; set; }
        public double? NetworkSendBytesPerSecond { get; set; }
        public long NetworkReceiveTotalBytes { get; set; }
        public long NetworkSendTotalBytes { get; set; }
        public int NetworkInterfaceCount { get; set; }
        /// <summary>
        /// 当前 Web 进程占运行时可见总 CPU 算力的百分比，归一化为 0-100。
        /// </summary>
        public double? ProcessCpuUsagePercent { get; set; }
        public long ProcessWorkingSetBytes { get; set; }
        public long ApplicationUptimeSeconds { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// 采集当前 Host 指标。仅在内存中保留上一组 CPU/网络计数器，用于计算两次采样间的实时速率。
    /// </summary>
    public sealed class HostMetricsCollector
    {
        private readonly object _syncRoot = new();
        private CpuCounterSnapshot? _previousCpu;
        private NetworkCounterSnapshot? _previousNetwork;
        private ProcessCounterSnapshot? _previousProcess;

        public HostMetricsSnapshot Collect()
        {
            lock (_syncRoot)
            {
                var snapshot = new HostMetricsSnapshot
                {
                    SampledAt = DateTimeOffset.UtcNow,
                    HostName = Environment.MachineName,
                    OperatingSystem = RuntimeInformation.OSDescription
                };

                CollectCpu(snapshot);
                CollectMemory(snapshot);
                CollectNetwork(snapshot);
                CollectProcess(snapshot);

                return snapshot;
            }
        }

        private void CollectCpu(HostMetricsSnapshot snapshot)
        {
            try
            {
                if (!TryReadCpuCounters(out var current))
                {
                    snapshot.Warnings.Add("当前操作系统暂不支持 Host CPU 采样。");
                    return;
                }

                if (_previousCpu.HasValue)
                {
                    var previous = _previousCpu.Value;
                    if (current.Total >= previous.Total && current.Idle >= previous.Idle)
                    {
                        var totalDelta = current.Total - previous.Total;
                        var idleDelta = current.Idle - previous.Idle;
                        if (totalDelta > 0 && idleDelta <= totalDelta)
                        {
                            snapshot.CpuUsagePercent = ClampPercentage(
                                (totalDelta - idleDelta) * 100d / totalDelta);
                        }
                    }
                }

                _previousCpu = current;
            }
            catch (Exception ex)
            {
                snapshot.Warnings.Add($"Host CPU 采样失败：{ex.Message}");
            }
        }

        private static void CollectMemory(HostMetricsSnapshot snapshot)
        {
            try
            {
                if (!TryReadMemory(out var totalBytes, out var availableBytes) || totalBytes <= 0)
                {
                    snapshot.Warnings.Add("当前操作系统暂不支持 Host 物理内存采样。");
                    return;
                }

                availableBytes = Math.Clamp(availableBytes, 0, totalBytes);
                snapshot.MemoryTotalBytes = totalBytes;
                snapshot.MemoryAvailableBytes = availableBytes;
                snapshot.MemoryUsedBytes = totalBytes - availableBytes;
                snapshot.MemoryUsagePercent = ClampPercentage(snapshot.MemoryUsedBytes * 100d / totalBytes);
            }
            catch (Exception ex)
            {
                snapshot.Warnings.Add($"Host 内存采样失败：{ex.Message}");
            }
        }

        private void CollectNetwork(HostMetricsSnapshot snapshot)
        {
            try
            {
                var current = ReadNetworkCounters();
                snapshot.NetworkReceiveTotalBytes = current.ReceivedBytes;
                snapshot.NetworkSendTotalBytes = current.SentBytes;
                snapshot.NetworkInterfaceCount = current.InterfaceCount;

                if (_previousNetwork.HasValue)
                {
                    var previous = _previousNetwork.Value;
                    var elapsedSeconds = (current.Timestamp - previous.Timestamp) / (double)Stopwatch.Frequency;
                    if (elapsedSeconds > 0 &&
                        current.ReceivedBytes >= previous.ReceivedBytes &&
                        current.SentBytes >= previous.SentBytes)
                    {
                        snapshot.NetworkReceiveBytesPerSecond =
                            (current.ReceivedBytes - previous.ReceivedBytes) / elapsedSeconds;
                        snapshot.NetworkSendBytesPerSecond =
                            (current.SentBytes - previous.SentBytes) / elapsedSeconds;
                    }
                }

                _previousNetwork = current;
            }
            catch (Exception ex)
            {
                snapshot.Warnings.Add($"Host 网络采样失败：{ex.Message}");
            }
        }

        private void CollectProcess(HostMetricsSnapshot snapshot)
        {
            try
            {
                using var process = Process.GetCurrentProcess();
                var current = new ProcessCounterSnapshot(
                    Stopwatch.GetTimestamp(),
                    process.TotalProcessorTime.Ticks);

                if (_previousProcess.HasValue)
                {
                    var previous = _previousProcess.Value;
                    var elapsedSeconds = (current.Timestamp - previous.Timestamp) / (double)Stopwatch.Frequency;
                    var processorTimeTicks = current.ProcessorTimeTicks - previous.ProcessorTimeTicks;
                    if (elapsedSeconds > 0 && processorTimeTicks >= 0)
                    {
                        var processorSeconds = TimeSpan.FromTicks(processorTimeTicks).TotalSeconds;
                        snapshot.ProcessCpuUsagePercent = ClampPercentage(
                            processorSeconds * 100d /
                            (elapsedSeconds * Math.Max(1, Environment.ProcessorCount)));
                    }
                }

                _previousProcess = current;
                snapshot.ProcessWorkingSetBytes = Math.Max(0, process.WorkingSet64);
                snapshot.ApplicationUptimeSeconds = Math.Max(
                    0,
                    (long)(DateTime.UtcNow - process.StartTime.ToUniversalTime()).TotalSeconds);
            }
            catch (Exception ex)
            {
                snapshot.Warnings.Add($"当前 Web 进程采样失败：{ex.Message}");
            }
        }

        private static bool TryReadCpuCounters(out CpuCounterSnapshot snapshot)
        {
            if (OperatingSystem.IsWindows())
            {
                return TryReadWindowsCpuCounters(out snapshot);
            }

            if (OperatingSystem.IsLinux())
            {
                return TryReadLinuxCpuCounters(out snapshot);
            }

            if (OperatingSystem.IsMacOS())
            {
                return TryReadMacCpuCounters(out snapshot);
            }

            snapshot = default;
            return false;
        }

        private static bool TryReadWindowsCpuCounters(out CpuCounterSnapshot snapshot)
        {
            if (!GetSystemTimes(out var idle, out var kernel, out var user))
            {
                snapshot = default;
                return false;
            }

            var idleValue = idle.ToUInt64();
            snapshot = new CpuCounterSnapshot(
                kernel.ToUInt64() + user.ToUInt64(),
                idleValue);
            return true;
        }

        private static bool TryReadLinuxCpuCounters(out CpuCounterSnapshot snapshot)
        {
            var line = File.ReadLines("/proc/stat").FirstOrDefault();
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("cpu ", StringComparison.Ordinal))
            {
                snapshot = default;
                return false;
            }

            var values = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Skip(1)
                .Take(8)
                .Select(value => ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
                    ? parsed
                    : 0UL)
                .ToArray();

            if (values.Length < 4)
            {
                snapshot = default;
                return false;
            }

            var total = values.Aggregate(0UL, (sum, value) => sum + value);
            var idle = values[3] + (values.Length > 4 ? values[4] : 0UL);
            snapshot = new CpuCounterSnapshot(total, idle);
            return true;
        }

        private static bool TryReadMacCpuCounters(out CpuCounterSnapshot snapshot)
        {
            var info = new MacHostCpuLoadInfo();
            var count = MacHostCpuLoadInfoCount;
            var result = HostStatistics64(MachHostSelf(), MacHostCpuLoadInfoFlavor, ref info, ref count);
            if (result != 0)
            {
                snapshot = default;
                return false;
            }

            snapshot = new CpuCounterSnapshot(
                (ulong)info.User + info.System + info.Idle + info.Nice,
                info.Idle);
            return true;
        }

        private static bool TryReadMemory(out long totalBytes, out long availableBytes)
        {
            if (OperatingSystem.IsWindows())
            {
                return TryReadWindowsMemory(out totalBytes, out availableBytes);
            }

            if (OperatingSystem.IsLinux())
            {
                return TryReadLinuxMemory(out totalBytes, out availableBytes);
            }

            if (OperatingSystem.IsMacOS())
            {
                return TryReadMacMemory(out totalBytes, out availableBytes);
            }

            totalBytes = 0;
            availableBytes = 0;
            return false;
        }

        private static bool TryReadWindowsMemory(out long totalBytes, out long availableBytes)
        {
            var status = new WindowsMemoryStatus
            {
                Length = (uint)Marshal.SizeOf<WindowsMemoryStatus>()
            };

            if (!GlobalMemoryStatusEx(ref status))
            {
                totalBytes = 0;
                availableBytes = 0;
                return false;
            }

            totalBytes = ToInt64(status.TotalPhysical);
            availableBytes = ToInt64(status.AvailablePhysical);
            return true;
        }

        private static bool TryReadLinuxMemory(out long totalBytes, out long availableBytes)
        {
            long totalKb = 0;
            long availableKb = 0;

            foreach (var line in File.ReadLines("/proc/meminfo"))
            {
                if (line.StartsWith("MemTotal:", StringComparison.Ordinal))
                {
                    totalKb = ParseLinuxMemoryKb(line);
                }
                else if (line.StartsWith("MemAvailable:", StringComparison.Ordinal))
                {
                    availableKb = ParseLinuxMemoryKb(line);
                }

                if (totalKb > 0 && availableKb > 0)
                {
                    break;
                }
            }

            totalBytes = totalKb > 0 ? checked(totalKb * 1024) : 0;
            availableBytes = availableKb > 0 ? checked(availableKb * 1024) : 0;
            return totalBytes > 0;
        }

        private static long ParseLinuxMemoryKb(string line)
        {
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 &&
                   long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0;
        }

        private static bool TryReadMacMemory(out long totalBytes, out long availableBytes)
        {
            nuint length = (nuint)sizeof(ulong);
            if (SysctlByName("hw.memsize", out var total, ref length, IntPtr.Zero, 0) != 0)
            {
                totalBytes = 0;
                availableBytes = 0;
                return false;
            }

            var host = MachHostSelf();
            var vmInfo = new MacVmStatistics64();
            var count = (uint)(Marshal.SizeOf<MacVmStatistics64>() / sizeof(int));
            if (HostStatistics64(host, MacHostVmInfo64Flavor, ref vmInfo, ref count) != 0 ||
                HostPageSize(host, out var pageSize) != 0)
            {
                totalBytes = 0;
                availableBytes = 0;
                return false;
            }

            var availablePages = (ulong)vmInfo.FreeCount +
                                 vmInfo.InactiveCount +
                                 vmInfo.SpeculativeCount +
                                 vmInfo.PurgeableCount;
            var available = availablePages * pageSize;
            totalBytes = ToInt64(total);
            availableBytes = ToInt64(Math.Min(total, available));
            return totalBytes > 0;
        }

        private static NetworkCounterSnapshot ReadNetworkCounters()
        {
            long receivedBytes = 0;
            long sentBytes = 0;
            var interfaceCount = 0;

            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus != OperationalStatus.Up ||
                    networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                    networkInterface.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                {
                    continue;
                }

                try
                {
                    var statistics = networkInterface.GetIPStatistics();
                    receivedBytes = SafeAdd(receivedBytes, Math.Max(0, statistics.BytesReceived));
                    sentBytes = SafeAdd(sentBytes, Math.Max(0, statistics.BytesSent));
                    interfaceCount++;
                }
                catch (NetworkInformationException)
                {
                    // 单个网卡临时不可用时跳过，避免影响其余网卡的汇总。
                }
            }

            return new NetworkCounterSnapshot(
                Stopwatch.GetTimestamp(),
                receivedBytes,
                sentBytes,
                interfaceCount);
        }

        private static long SafeAdd(long left, long right)
        {
            return left > long.MaxValue - right ? long.MaxValue : left + right;
        }

        private static long ToInt64(ulong value)
        {
            return value > long.MaxValue ? long.MaxValue : (long)value;
        }

        private static double ClampPercentage(double value)
        {
            return Math.Round(Math.Clamp(value, 0d, 100d), 2);
        }

        private readonly record struct CpuCounterSnapshot(ulong Total, ulong Idle);

        private readonly record struct NetworkCounterSnapshot(
            long Timestamp,
            long ReceivedBytes,
            long SentBytes,
            int InterfaceCount);

        private readonly record struct ProcessCounterSnapshot(
            long Timestamp,
            long ProcessorTimeTicks);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeFileTime
        {
            public uint Low;
            public uint High;

            public readonly ulong ToUInt64()
            {
                return ((ulong)High << 32) | Low;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowsMemoryStatus
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhysical;
            public ulong AvailablePhysical;
            public ulong TotalPageFile;
            public ulong AvailablePageFile;
            public ulong TotalVirtual;
            public ulong AvailableVirtual;
            public ulong AvailableExtendedVirtual;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MacHostCpuLoadInfo
        {
            public uint User;
            public uint System;
            public uint Idle;
            public uint Nice;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MacVmStatistics64
        {
            public uint FreeCount;
            public uint ActiveCount;
            public uint InactiveCount;
            public uint WireCount;
            public ulong ZeroFillCount;
            public ulong Reactivations;
            public ulong PageIns;
            public ulong PageOuts;
            public ulong Faults;
            public ulong CopyOnWriteFaults;
            public ulong Lookups;
            public ulong Hits;
            public ulong Purges;
            public uint PurgeableCount;
            public uint SpeculativeCount;
            public ulong Decompressions;
            public ulong Compressions;
            public ulong SwapIns;
            public ulong SwapOuts;
            public uint CompressorPageCount;
            public uint ThrottledCount;
            public uint ExternalPageCount;
            public uint InternalPageCount;
            public ulong TotalUncompressedPagesInCompressor;
        }

        private const string WindowsKernelLibrary = "kernel32.dll";
        private const string MacSystemLibrary = "/usr/lib/libSystem.B.dylib";
        private const int MacHostCpuLoadInfoFlavor = 3;
        private const int MacHostVmInfo64Flavor = 4;
        private const uint MacHostCpuLoadInfoCount = 4;

        [DllImport(WindowsKernelLibrary, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetSystemTimes(
            out NativeFileTime idleTime,
            out NativeFileTime kernelTime,
            out NativeFileTime userTime);

        [DllImport(WindowsKernelLibrary, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref WindowsMemoryStatus buffer);

        [DllImport(MacSystemLibrary, EntryPoint = "mach_host_self")]
        private static extern uint MachHostSelf();

        [DllImport(MacSystemLibrary, EntryPoint = "host_statistics64")]
        private static extern int HostStatistics64(
            uint host,
            int flavor,
            ref MacHostCpuLoadInfo info,
            ref uint count);

        [DllImport(MacSystemLibrary, EntryPoint = "host_statistics64")]
        private static extern int HostStatistics64(
            uint host,
            int flavor,
            ref MacVmStatistics64 info,
            ref uint count);

        [DllImport(MacSystemLibrary, EntryPoint = "host_page_size")]
        private static extern int HostPageSize(uint host, out uint pageSize);

        [DllImport(MacSystemLibrary, EntryPoint = "sysctlbyname")]
        private static extern int SysctlByName(
            [MarshalAs(UnmanagedType.LPStr)] string name,
            out ulong oldValue,
            ref nuint oldLength,
            IntPtr newValue,
            nuint newLength);
    }
}
