using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Infrastructure.AutoStart;
using AutoSerialPort.Infrastructure.Factories;
using AutoSerialPort.Infrastructure.Logging;
using AutoSerialPort.Infrastructure.Parsing;
using AutoSerialPort.Infrastructure.Framing;
using AutoSerialPort.Infrastructure.Persistence;
using AutoSerialPort.Infrastructure.Serial;
using AutoSerialPort.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;

namespace AutoSerialPort.Infrastructure;

/// <summary>
/// Infrastructure 层依赖注入扩展。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册基础设施层服务。
    /// </summary>
    /// <param name="services">服务集合。</param>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // 路径服务为单例，避免多次创建目录
        var pathService = new AppPathService();
        services.AddSingleton<IAppPathService>(pathService);

        // 日志缓冲区用于 UI 展示
        var logBuffer = new LogBuffer(500);
        services.AddSingleton<ILogBuffer>(logBuffer);

        // 初始化 Serilog
        var logger = LoggingConfigurator.Configure(pathService, logBuffer);
        services.AddSingleton(logger);

        // 注册 Microsoft.Extensions.Logging 以支持 ILogger<T> 注入
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(new SerilogLoggerProvider(logger, dispose: false));
        });

        // 初始化数据库上下文
        var db = SqlSugarFactory.Create(pathService);
        services.AddSingleton(db);
        services.AddSingleton<SqlSugarInitializer>();
        services.AddSingleton<IConfigRepository, SqlSugarConfigRepository>();

        // 系统与硬件相关服务
        services.AddSingleton<IAutoStartService, AutoStartService>();
        services.AddSingleton<ITypingService, TypingService>();
        services.AddSingleton<ISerialPortDiscoveryService, SerialPortDiscoveryService>();

        // 解析器、拆包与工厂
        services.AddSingleton<IParserProvider, LineParserProvider>();
        services.AddSingleton<IParserProvider, JsonFieldParserProvider>();
        services.AddSingleton<IParserProvider, ScaleParserProvider>();
        services.AddSingleton<IParserProvider, BarcodeParserProvider>();
        services.AddSingleton<IFrameDecoderProvider, DelimiterFrameDecoderProvider>();
        services.AddSingleton<IFrameDecoderProvider, HeaderFooterFrameDecoderProvider>();
        services.AddSingleton<IFrameDecoderProvider, FixedLengthFrameDecoderProvider>();
        services.AddSingleton<IFrameDecoderProvider, NoFrameDecoderProvider>();
        services.AddSingleton<ParserFactory>();
        services.AddSingleton<FrameDecoderFactory>();
        services.AddSingleton<ForwarderFactory>();
        services.AddSingleton<ISerialPortLocator, SerialPortLocator>();
        services.AddSingleton<ISerialService, SerialPipelineService>();

        return services;
    }
}
