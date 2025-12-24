using System;
using Microsoft.Extensions.DependencyInjection;

namespace AutoSerialPort.Application;

/// <summary>
/// 应用级服务定位器，提供宿主初始化后的全局获取入口。
/// </summary>
public static class AppServices
{
    /// <summary>
    /// 当前全局服务提供器。
    /// </summary>
    public static IServiceProvider? Provider { get; private set; }

    /// <summary>
    /// 初始化全局服务提供器。
    /// </summary>
    /// <param name="provider">依赖注入容器。</param>
    public static void Initialize(IServiceProvider provider)
    {
        Provider = provider;
    }

    /// <summary>
    /// 获取必需服务实例。
    /// </summary>
    /// <typeparam name="T">服务类型。</typeparam>
    public static T GetRequired<T>() where T : notnull
    {
        if (Provider == null)
        {
            throw new InvalidOperationException("Service provider not initialized");
        }

        return Provider.GetRequiredService<T>();
    }
}
