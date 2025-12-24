using System;
using System.IO;
using System.Text;
using AutoSerialPort.Domain.Abstractions;
using Serilog;
using Serilog.Events;

namespace AutoSerialPort.Infrastructure.Logging;

/// <summary>
/// Serilog 配置入口。
/// 提供增强的日志配置，支持设备选择状态管理的调试和故障排除。
/// </summary>
public static class LoggingConfigurator
{
    /// <summary>
    /// 初始化日志配置并返回 logger。
    /// </summary>
    /// <param name="pathService">路径服务。</param>
    /// <param name="buffer">UI 日志缓冲区。</param>
    public static ILogger Configure(IAppPathService pathService, ILogBuffer buffer)
    {
        // 尝试设置控制台输出编码为 UTF-8（某些环境可能不支持）
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        try
        {
            Console.OutputEncoding = utf8;
        }
        catch
        {
            // 忽略编码设置失败，继续使用默认编码
        }

        try
        {
            Console.InputEncoding = utf8;
        }
        catch
        {
            // 忽略编码设置失败，继续使用默认编码
        }

        try
        {
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput(), utf8) { AutoFlush = true });
        }
        catch
        {
            // 忽略输出重定向失败，继续使用默认输出
        }

        try
        {
            Console.SetError(new StreamWriter(Console.OpenStandardError(), utf8) { AutoFlush = true });
        }
        catch
        {
            // 忽略错误输出重定向失败，继续使用默认输出
        }

        // 确保日志目录存在
        Directory.CreateDirectory(pathService.LogDirectory);

        // 检查是否启用调试模式
        var isDebugMode = IsDebugModeEnabled();
        var minimumLevel = isDebugMode ? LogEventLevel.Debug : LogEventLevel.Information;

        var logPath = Path.Combine(pathService.LogDirectory, "autoserial-.log");
        var debugLogPath = Path.Combine(pathService.LogDirectory, "debug", "autoserial-debug-.log");

        // 确保调试日志目录存在
        if (isDebugMode)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(debugLogPath)!);
        }

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "AutoSerialPort")
            .Enrich.WithProperty("Version", GetApplicationVersion())
            .WriteTo.Console(
                restrictedToMinimumLevel: LogEventLevel.Information,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                restrictedToMinimumLevel: LogEventLevel.Information,
                encoding: Encoding.UTF8,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.Sink(new InMemoryLogSink(buffer));

        // 添加调试日志文件（如果启用调试模式）
        if (isDebugMode)
        {
            loggerConfig = loggerConfig
                .WriteTo.File(
                    debugLogPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    restrictedToMinimumLevel: LogEventLevel.Debug,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Properties}: {Message:lj}{NewLine}{Exception}")
                .WriteTo.Logger(deviceSelectionLogger => deviceSelectionLogger
                    .Filter.ByIncludingOnly(evt => 
                    {
                        // 安全地检查 SourceContext 属性是否存在
                        if (!evt.Properties.TryGetValue("SourceContext", out var sourceContext))
                        {
                            return false;
                        }
                        
                        var contextValue = sourceContext.ToString();
                        return contextValue.Contains("SelectionStateManager") ||
                               contextValue.Contains("DeviceDataManager") ||
                               contextValue.Contains("DeviceCache");
                    })
                    .WriteTo.File(
                        Path.Combine(pathService.LogDirectory, "debug", "device-selection-.log"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        restrictedToMinimumLevel: LogEventLevel.Debug,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj} {Properties}{NewLine}{Exception}"));
        }

        var logger = loggerConfig.CreateLogger();

        // 替换全局日志实例
        Log.Logger = logger;
        
        // 记录日志配置信息
        logger.Information("日志系统已初始化 - 调试模式: {DebugMode}, 最小级别: {MinimumLevel}", isDebugMode, minimumLevel);
        
        return logger;
    }

    /// <summary>
    /// 检查是否启用调试模式
    /// </summary>
    /// <returns>是否启用调试模式</returns>
    private static bool IsDebugModeEnabled()
    {
        // 检查环境变量
        var debugEnv = Environment.GetEnvironmentVariable("AUTOSERIAL_DEBUG");
        if (!string.IsNullOrEmpty(debugEnv) && 
            (debugEnv.Equals("true", StringComparison.OrdinalIgnoreCase) || debugEnv == "1"))
        {
            return true;
        }

        // 检查命令行参数
        var args = Environment.GetCommandLineArgs();
        if (args.Contains("--debug") || args.Contains("-d"))
        {
            return true;
        }

        // 在Debug构建中默认启用
#if DEBUG
        return true;
#else
        return false;
#endif
    }

    /// <summary>
    /// 获取应用程序版本
    /// </summary>
    /// <returns>应用程序版本字符串</returns>
    private static string GetApplicationVersion()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            var version = assembly?.GetName().Version;
            return version?.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
}
