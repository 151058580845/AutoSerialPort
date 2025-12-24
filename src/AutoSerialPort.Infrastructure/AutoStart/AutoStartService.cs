using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using AutoSerialPort.Domain.Abstractions;
using Microsoft.Win32;

namespace AutoSerialPort.Infrastructure.AutoStart;

/// <summary>
/// 自动启动服务实现，支持 Windows 注册表与 Linux Autostart。
/// </summary>
public class AutoStartService : IAutoStartService
{
    private const string AppName = "AutoSerialPort";

    /// <summary>
    /// 获取是否启用自动启动。
    /// </summary>
    public Task<bool> GetAutoStartEnabledAsync()
    {
        if (OperatingSystem.IsWindows())
        {
            // Windows：读取注册表 Run
            using var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", false);
            var value = key?.GetValue(AppName) as string;
            return Task.FromResult(!string.IsNullOrWhiteSpace(value));
        }

        if (OperatingSystem.IsLinux())
        {
            // Linux：检查 Autostart 文件
            var desktopFile = GetLinuxDesktopFilePath();
            return Task.FromResult(File.Exists(desktopFile));
        }

        return Task.FromResult(false);
    }

    /// <summary>
    /// 设置是否启用自动启动。
    /// </summary>
    /// <param name="enabled">是否启用。</param>
    public Task SetAutoStartEnabledAsync(bool enabled)
    {
        if (OperatingSystem.IsWindows())
        {
            using var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (enabled)
            {
                // 注册启动项
                key?.SetValue(AppName, GetExecutablePath());
            }
            else
            {
                // 删除启动项
                key?.DeleteValue(AppName, false);
            }

            return Task.CompletedTask;
        }

        if (OperatingSystem.IsLinux())
        {
            var desktopFile = GetLinuxDesktopFilePath();
            if (enabled)
            {
                // 生成 Autostart desktop 文件
                Directory.CreateDirectory(Path.GetDirectoryName(desktopFile)!);
                var execPath = GetExecutablePath();
                if (execPath.Contains(' '))
                {
                    execPath = $"\"{execPath}\"";
                }

                var content = $"[Desktop Entry]{Environment.NewLine}" +
                              $"Type=Application{Environment.NewLine}" +
                              $"Name={AppName}{Environment.NewLine}" +
                              $"Exec={execPath}{Environment.NewLine}" +
                              "X-GNOME-Autostart-enabled=true" + Environment.NewLine;
                File.WriteAllText(desktopFile, content);
            }
            else if (File.Exists(desktopFile))
            {
                // 关闭自动启动
                File.Delete(desktopFile);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取 Linux Autostart desktop 文件路径。
    /// </summary>
    private static string GetLinuxDesktopFilePath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        return Path.Combine(home, ".config", "autostart", "AutoSerialPort.desktop");
    }

    /// <summary>
    /// 获取当前可执行文件路径。
    /// </summary>
    private static string GetExecutablePath()
    {
        var path = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        return Process.GetCurrentProcess().MainModule?.FileName ?? "AutoSerialPort";
    }
}
