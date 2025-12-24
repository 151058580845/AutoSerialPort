using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Models;

namespace AutoSerialPort.Infrastructure.Services;

/// <summary>
/// 串口扫描服务实现，支持 Windows 与 Linux 的端口识别。
/// </summary>
public class SerialPortDiscoveryService : ISerialPortDiscoveryService
{
    /// <summary>
    /// 获取可用串口列表。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    public Task<IReadOnlyList<SerialPortDescriptor>> GetAvailablePortsAsync(CancellationToken ct)
    {
        if (OperatingSystem.IsWindows())
        {
            return Task.FromResult<IReadOnlyList<SerialPortDescriptor>>(GetWindowsPorts());
        }

        if (OperatingSystem.IsLinux())
        {
            return Task.FromResult<IReadOnlyList<SerialPortDescriptor>>(GetLinuxPorts());
        }

        return Task.FromResult<IReadOnlyList<SerialPortDescriptor>>(GetGenericPorts());
    }

    /// <summary>
    /// 使用系统默认方式获取串口列表。
    /// </summary>
    private static IReadOnlyList<SerialPortDescriptor> GetGenericPorts()
    {
        return SerialPort.GetPortNames()
            .Select(name => new SerialPortDescriptor
            {
                PortName = name,
                DisplayName = name
            })
            .ToList();
    }

    /// <summary>
    /// Linux 下补充 /dev/serial/by-id 的稳定标识。
    /// </summary>
    private static IReadOnlyList<SerialPortDescriptor> GetLinuxPorts()
    {
        var descriptors = GetGenericPorts().ToList();
        var byIdDir = "/dev/serial/by-id";
        if (!Directory.Exists(byIdDir))
        {
            return descriptors;
        }

        foreach (var path in Directory.GetFiles(byIdDir))
        {
            var target = ResolveLink(path);
            if (string.IsNullOrWhiteSpace(target))
            {
                continue;
            }

            var existing = descriptors.FirstOrDefault(x => string.Equals(x.PortName, target, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.ByIdPath = path;
                continue;
            }

            descriptors.Add(new SerialPortDescriptor
            {
                PortName = target,
                DisplayName = Path.GetFileName(path) ?? target,
                ByIdPath = path
            });
        }

        return descriptors;
    }

    /// <summary>
    /// 解析符号链接真实路径。
    /// </summary>
    /// <param name="path">链接路径。</param>
    private static string? ResolveLink(string path)
    {
        try
        {
            var info = new FileInfo(path);
            var target = info.ResolveLinkTarget(true);
            return target?.FullName;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Windows 下通过 WMI 获取串口描述与设备信息。
    /// </summary>
    private static IReadOnlyList<SerialPortDescriptor> GetWindowsPorts()
    {
        var results = new Dictionary<string, SerialPortDescriptor>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT Name, PNPDeviceID FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");
            foreach (var item in searcher.Get())
            {
                var name = item["Name"]?.ToString();
                var pnp = item["PNPDeviceID"]?.ToString();
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(pnp))
                {
                    continue;
                }

                var port = ParseComPort(name);
                if (string.IsNullOrWhiteSpace(port))
                {
                    continue;
                }

                var vidPid = ParseVidPid(pnp);
                results[port] = new SerialPortDescriptor
                {
                    PortName = port,
                    DisplayName = name,
                    PnpDeviceId = pnp,
                    VidPid = vidPid
                };
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        try
        {
            var systemPorts = SerialPort.GetPortNames();
            foreach (var port in systemPorts)
            {
                if (!results.ContainsKey(port))
                {
                    results[port] = new SerialPortDescriptor()
                    {
                        PortName = port,
                        DisplayName = port,
                        PnpDeviceId = null,
                        VidPid = null
                    };
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        return results.Values.OrderBy(x=>x.PortName,new ComPortComparer()).ToList();
    }

    /// <summary>
    /// 从字符串中提取 COM 端口号。
    /// </summary>
    /// <param name="name">设备名称。</param>
    private static string? ParseComPort(string name)
    {
        var match = Regex.Match(name, "\\((COM\\d+)\\)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// 从 PNP 设备 ID 提取 VID/PID。
    /// </summary>
    /// <param name="pnp">PNP 设备 ID。</param>
    private static string? ParseVidPid(string pnp)
    {
        var match = Regex.Match(pnp, "VID_([0-9A-Fa-f]{4})&PID_([0-9A-Fa-f]{4})");
        if (match.Success)
        {
            return $"VID_{match.Groups[1].Value}&PID_{match.Groups[2].Value}";
        }

        return null;
    }
    
    /// <summary>
    /// COM端口名称比较器
    /// </summary>
    private class ComPortComparer:IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var xMatch = Regex.Match(x, @"COM(\d+)", RegexOptions.IgnoreCase);
            var yMatch = Regex.Match(x, @"COM(\d+)", RegexOptions.IgnoreCase);
            if (xMatch.Success && yMatch.Success)
            {
                var xNum = int.Parse(xMatch.Groups[1].Value);
                var yNum = int.Parse(yMatch.Groups[1].Value);
                return xNum.CompareTo(yNum);
            }

            return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
        }
    }
}
