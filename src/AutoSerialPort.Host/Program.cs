using System.Text;
using Avalonia;
using AutoSerialPort.Application;
using AutoSerialPort.Application.Abstractions;
using AutoSerialPort.Infrastructure;
using AutoSerialPort.Infrastructure.Persistence;
using AutoSerialPort.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoSerialPort.Host;

/// <summary>
/// 程序入口。
/// </summary>
public static class Program
{
    /// <summary>
    /// 应用主入口，初始化依赖注入与数据库。
    /// </summary>
    /// <param name="args">命令行参数。</param>
    [STAThread]
    public static void Main(string[] args)
    {
        // 设置控制台编码为 UTF-8，解决中文乱码问题
        // Console.OutputEncoding = Encoding.UTF8;
        // Console.InputEncoding = Encoding.UTF8;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var services = new ServiceCollection();
        // 注册各层服务
        services.AddInfrastructureServices();
        services.AddApplicationServices();
        services.AddUiServices();

        var provider = services.BuildServiceProvider();
        AppServices.Initialize(provider);

        // 初始化数据库（同步执行，数据量小无需异步）
        var initializer = provider.GetRequiredService<SqlSugarInitializer>();
        initializer.Initialize();

        // 启动 Avalonia
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// 构建 Avalonia AppBuilder。
    /// </summary>
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}