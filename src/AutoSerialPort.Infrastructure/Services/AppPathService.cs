using System;
using System.IO;
using AutoSerialPort.Domain.Abstractions;

namespace AutoSerialPort.Infrastructure.Services;

/// <summary>
/// 应用路径服务实现，提供统一的数据与日志目录。
/// </summary>
public class AppPathService : IAppPathService
{
    /// <summary>
    /// 应用数据目录。
    /// </summary>
    public string AppDataDirectory { get; }

    /// <summary>
    /// 数据库文件路径。
    /// </summary>
    public string DatabasePath { get; }

    /// <summary>
    /// 日志目录。
    /// </summary>
    public string LogDirectory { get; }

    /// <summary>
    /// 初始化路径并创建必要目录。
    /// </summary>
    public AppPathService()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(root))
        {
            root = AppContext.BaseDirectory;
        }

        // 应用目录统一放在用户数据路径下
        AppDataDirectory = Path.Combine(root, "AutoSerialPort");
        Directory.CreateDirectory(AppDataDirectory);

        // 数据库与日志目录
        DatabasePath = Path.Combine(AppDataDirectory, "autoserial.db");
        LogDirectory = Path.Combine(AppDataDirectory, "logs");
        Directory.CreateDirectory(LogDirectory);
    }
}
