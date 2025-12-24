using AutoSerialPort.Application.Abstractions;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.UI.Services;
using AutoSerialPort.UI.ViewModels;
using AutoSerialPort.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AutoSerialPort.UI;

/// <summary>
/// UI 层依赖注入扩展。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册 UI 层服务与视图模型。
    /// </summary>
    /// <param name="services">服务集合。</param>
    public static IServiceCollection AddUiServices(this IServiceCollection services)
    {
        // 首先注册工厂，供Application层使用
        services.AddSingleton<Func<IDeviceProfileViewModel>>(sp => () => sp.GetRequiredService<DeviceProfileViewModel>());
        
        // UI 剪贴板服务
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IUiApplyService, UiApplyService>();

        // 视图模型注册
        services.AddTransient<SerialSettingsViewModel>();
        services.AddTransient<ParserSettingsViewModel>();
        services.AddTransient<ForwarderSettingsViewModel>();
        services.AddTransient<DeviceProfileViewModel>();
        services.AddSingleton<LogViewModel>();
        services.AddTransient<StatusBarViewModel>();
        services.AddSingleton<SerialConsoleViewModel>();
        services.AddTransient<MainWindowViewModel>();

        // 视图与工厂注册
        services.AddTransient<MainWindow>();
        services.AddSingleton<Func<MainWindow>>(sp => () => sp.GetRequiredService<MainWindow>());
        services.AddSingleton<Func<DeviceProfileViewModel>>(sp => () => sp.GetRequiredService<DeviceProfileViewModel>());
        services.AddSingleton<TrayViewModel>();

        return services;
    }
}
