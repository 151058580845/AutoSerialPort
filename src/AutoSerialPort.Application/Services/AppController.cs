using System.Threading;
using System.Threading.Tasks;
using AutoSerialPort.Application.Abstractions;
using AutoSerialPort.Application.Models;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Models;

namespace AutoSerialPort.Application.Services;

/// <summary>
/// 应用控制器实现，负责配置持久化与后台服务联动。
/// </summary>
public class AppController : IAppController
{
    private readonly IConfigRepository _configRepository;
    private readonly ISerialService _serialService;
    private readonly IAutoStartService _autoStartService;

    public AppController(
        IConfigRepository configRepository,
        ISerialService serialService,
        IAutoStartService autoStartService)
    {
        _configRepository = configRepository;
        _serialService = serialService;
        _autoStartService = autoStartService;
    }

    /// <summary>
    /// 加载应用状态与设备配置。
    /// </summary>
    public async Task<AppState> LoadAsync()
    {
        // 确保默认方案存在，并使用它作为初始方案
        var profileId = await _configRepository.EnsureDefaultProfileAsync();
        var settings = await _configRepository.GetAppSettingsAsync();
        if (settings.LastProfileId == 0)
        {
            settings.LastProfileId = profileId;
        }

        // 从仓储加载设备配置与方案列表
        var devices = await _configRepository.GetSerialDeviceProfilesAsync(settings.LastProfileId);
        var profiles = await _configRepository.GetProfilesAsync();

        return new AppState
        {
            Settings = settings,
            Devices = devices,
            Profiles = profiles
        };
    }

    /// <summary>
    /// 保存配置并通知后台服务应用新配置。
    /// </summary>
    /// <param name="state">当前 UI 状态。</param>
    public async Task ApplyAsync(AppState state)
    {
        // 先持久化配置
        await _configRepository.SaveAppSettingsAsync(state.Settings);
        await _configRepository.SaveSerialDeviceProfilesAsync(state.Settings.LastProfileId, state.Devices);

        // 再通知后台服务热更新
        await _serialService.ApplyConfigAsync(state.Devices, CancellationToken.None);

        // 最后同步自动启动状态
        await _autoStartService.SetAutoStartEnabledAsync(state.Settings.AutoStart);
    }

    /// <summary>
    /// 启动后台采集。
    /// </summary>
    public Task StartAsync() => _serialService.StartAsync(CancellationToken.None);

    /// <summary>
    /// 停止后台采集。
    /// </summary>
    public Task StopAsync() => _serialService.StopAsync(CancellationToken.None);

    /// <summary>
    /// 获取当前采集状态快照。
    /// </summary>
    public SerialStatusSnapshot GetStatus() => _serialService.GetStatus();

    /// <summary>
    /// 获取自动启动开关状态。
    /// </summary>
    public Task<bool> GetAutoStartAsync() => _autoStartService.GetAutoStartEnabledAsync();

    /// <summary>
    /// 设置自动启动并同步保存到配置。
    /// </summary>
    /// <param name="enabled">是否启用。</param>
    public async Task SetAutoStartAsync(bool enabled)
    {
        await _autoStartService.SetAutoStartEnabledAsync(enabled);
        var settings = await _configRepository.GetAppSettingsAsync();
        settings.AutoStart = enabled;
        await _configRepository.SaveAppSettingsAsync(settings);
    }

    /// <summary>
    /// 手动启动指定设备。
    /// </summary>
    /// <param name="deviceId">设备 ID。</param>
    public Task<SerialOperationResult> StartDeviceAsync(long deviceId)
        => _serialService.StartDeviceAsync(deviceId, CancellationToken.None);

    /// <summary>
    /// 手动停止指定设备。
    /// </summary>
    /// <param name="deviceId">设备 ID。</param>
    public Task<SerialOperationResult> StopDeviceAsync(long deviceId)
        => _serialService.StopDeviceAsync(deviceId, CancellationToken.None);
}
