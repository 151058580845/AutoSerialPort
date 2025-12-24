using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Entities;
using AutoSerialPort.Domain.Models;
using Serilog;

namespace AutoSerialPort.Infrastructure.Services;

/// <summary>
/// 串口定位服务实现，根据标识解析实际端口名。
/// </summary>
public class SerialPortLocator : ISerialPortLocator
{
    /// <summary>
    /// 根据配置解析当前可用的串口名称。
    /// </summary>
    /// <param name="config">设备配置。</param>
    /// <param name="ct">取消令牌。</param>
    public Task<string?> ResolvePortNameAsync(SerialDeviceConfig config, CancellationToken ct)
    {
        if (string.Equals(config.IdentifierType, SerialIdentifierTypes.PortName, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<string?>(config.IdentifierValue);
        }

        if (string.Equals(config.IdentifierType, SerialIdentifierTypes.ByIdPath, StringComparison.OrdinalIgnoreCase))
        {
            // Linux 通过稳定路径定位
            return Task.FromResult(File.Exists(config.IdentifierValue) ? config.IdentifierValue : null);
        }

        if (OperatingSystem.IsWindows())
        {
            if (string.Equals(config.IdentifierType, SerialIdentifierTypes.PnpDeviceId, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(FindPortByPnpId(config.IdentifierValue));
            }

            if (string.Equals(config.IdentifierType, SerialIdentifierTypes.UsbVidPid, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(FindPortByVidPid(config.IdentifierValue));
            }
        }

        if (OperatingSystem.IsLinux() && string.Equals(config.IdentifierType, SerialIdentifierTypes.UsbVidPid, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(FindLinuxById(config.IdentifierValue));
        }

        return Task.FromResult<string?>(config.IdentifierValue);
    }

    /// <summary>
    /// Linux 下通过 /dev/serial/by-id 查找设备。
    /// </summary>
    /// <param name="id">设备标识。</param>
    private static string? FindLinuxById(string id)
    {
        var dir = "/dev/serial/by-id";
        if (!Directory.Exists(dir))
        {
            return null;
        }

        var match = Directory.GetFiles(dir)
            .FirstOrDefault(path => path.Contains(id, StringComparison.OrdinalIgnoreCase));

        return match;
    }

    /// <summary>
    /// Windows 下通过 VID/PID 查找端口。
    /// </summary>
    private static string? FindPortByVidPid(string vidPid)
    {
        return FindPortByPnpContains(vidPid);
    }

    /// <summary>
    /// Windows 下通过 PNP ID 查找端口。
    /// </summary>
    private static string? FindPortByPnpId(string pnpId)
    {
        return FindPortByPnpContains(pnpId);
    }

    /// <summary>
    /// Windows 下通过 PNP 关键字模糊匹配端口。
    /// </summary>
    private static string? FindPortByPnpContains(string keyword)
    {
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

                if (!pnp.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var port = ParseComPort(name);
                if (!string.IsNullOrWhiteSpace(port))
                {
                    return port;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to resolve COM port by PNP ID");
        }

        return null;
    }

    /// <summary>
    /// 从名称中解析 COM 端口号。
    /// </summary>
    /// <param name="name">设备名称。</param>
    private static string? ParseComPort(string name)
    {
        var match = Regex.Match(name, "\\((COM\\d+)\\)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return null;
    }
}
