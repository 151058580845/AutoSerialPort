using AutoSerialPort.Application.Abstractions;
using AutoSerialPort.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AutoSerialPort.Application;

/// <summary>
/// Application 层依赖注入扩展。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Application 层服务。
    /// </summary>
    /// <param name="services">服务集合。</param>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // 控制器单例即可
        services.AddSingleton<IAppController, AppController>();
        
        // 设备选择状态管理服务
        services.AddSingleton<IDeviceCache, DeviceCache>();
        services.AddSingleton<ISelectionStateManager, SelectionStateManager>();
        
        // DeviceDataManager需要工厂，将在UI层注册时提供
        services.AddSingleton<IDeviceDataManager>(serviceProvider =>
        {
            var configRepository = serviceProvider.GetRequiredService<Domain.Abstractions.IConfigRepository>();
            var deviceCache = serviceProvider.GetRequiredService<IDeviceCache>();
            var selectionStateManager = serviceProvider.GetRequiredService<ISelectionStateManager>();
            var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DeviceDataManager>>();
            
            // 尝试获取UI层提供的工厂，如果没有则使用默认实现
            var factory = serviceProvider.GetService<Func<IDeviceProfileViewModel>>();
            if (factory == null)
            {
                // 提供默认的轻量级实现
                factory = () => new ViewModels.DeviceProfileViewModel();
            }
            
            return new DeviceDataManager(configRepository, deviceCache, selectionStateManager, factory, logger);
        });
        
        return services;
    }
}
