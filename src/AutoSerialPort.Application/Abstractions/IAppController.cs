using System.Threading.Tasks;
using AutoSerialPort.Application.Models;
using AutoSerialPort.Domain.Models;

namespace AutoSerialPort.Application.Abstractions;

/// <summary>
/// 应用控制器接口，封装 UI 与后台服务的交互。
/// </summary>
public interface IAppController
{
    /// <summary>
    /// 加载应用状态与配置。
    /// </summary>
    Task<AppState> LoadAsync();

    /// <summary>
    /// 应用当前 UI 配置。
    /// </summary>
    /// <param name="state">界面状态。</param>
    Task ApplyAsync(AppState state);

    /// <summary>
    /// 启动后台采集与转发。
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// 停止后台采集与转发。
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// 获取运行状态快照。
    /// </summary>
    SerialStatusSnapshot GetStatus();

    /// <summary>
    /// 获取是否启用自动启动。
    /// </summary>
    Task<bool> GetAutoStartAsync();

    /// <summary>
    /// 设置是否启用自动启动。
    /// </summary>
    /// <param name="enabled">是否启用。</param>
    Task SetAutoStartAsync(bool enabled);

    /// <summary>
    /// 手动启动指定设备。
    /// </summary>
    /// <param name="deviceId">设备 ID。</param>
    Task<SerialOperationResult> StartDeviceAsync(long deviceId);

    /// <summary>
    /// 手动停止指定设备。
    /// </summary>
    /// <param name="deviceId">设备 ID。</param>
    Task<SerialOperationResult> StopDeviceAsync(long deviceId);
}
