using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AutoSerialPort.Application.Abstractions;
using AutoSerialPort.Application.Models;

namespace AutoSerialPort.Application.Services;

/// <summary>
/// 设备选择状态管理器实现
/// 简化版本：移除防抖和复杂的错误恢复机制
/// </summary>
public class SelectionStateManager : ISelectionStateManager
{
    private readonly IDeviceCache _deviceCache;
    private readonly ILogger<SelectionStateManager> _logger;
    private readonly object _lockObject = new();

    private IDeviceProfileViewModel? _selectedDevice;

    public SelectionStateManager(IDeviceCache deviceCache, ILogger<SelectionStateManager> logger)
    {
        _deviceCache = deviceCache ?? throw new ArgumentNullException(nameof(deviceCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IDeviceProfileViewModel? SelectedDevice
    {
        get
        {
            lock (_lockObject)
            {
                return _selectedDevice;
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<DeviceSelectionChangedEventArgs>? SelectionChanged;

    /// <inheritdoc />
    public Task<bool> SelectDeviceAsync(long deviceId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SelectDeviceAsync 被调用: DeviceId={DeviceId}", deviceId);
        
        if (deviceId <= 0)
        {
            _logger.LogWarning("无效的设备ID: {DeviceId}", deviceId);
            return Task.FromResult(false);
        }

        var device = _deviceCache.GetById(deviceId);
        if (device == null)
        {
            _logger.LogWarning("未找到设备，设备ID: {DeviceId}", deviceId);
            return Task.FromResult(false);
        }

        _logger.LogInformation("从缓存获取到设备: DeviceId={DeviceId}, DisplayName={DisplayName}", 
            device.DeviceId, device.DisplayName);
        return Task.FromResult(SelectDeviceInternal(device, SelectionChangeReason.UserSelection));
    }

    /// <inheritdoc />
    public Task<bool> SelectDeviceAsync(string displayName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            _logger.LogWarning("显示名称不能为空");
            return Task.FromResult(false);
        }

        var device = _deviceCache.GetByDisplayName(displayName);
        if (device == null)
        {
            _logger.LogWarning("未找到设备，显示名称: {DisplayName}", displayName);
            return Task.FromResult(false);
        }

        return Task.FromResult(SelectDeviceInternal(device, SelectionChangeReason.UserSelection));
    }

    /// <inheritdoc />
    public Task<bool> SelectDeviceAsync(IDeviceProfileViewModel device, CancellationToken cancellationToken = default)
    {
        if (device == null)
        {
            _logger.LogWarning("设备对象不能为空");
            return Task.FromResult(false);
        }

        return Task.FromResult(SelectDeviceInternal(device, SelectionChangeReason.UserSelection));
    }

    /// <summary>
    /// 内部选择设备方法
    /// </summary>
    private bool SelectDeviceInternal(IDeviceProfileViewModel device, SelectionChangeReason reason)
    {
        lock (_lockObject)
        {
            _logger.LogInformation("SelectDeviceInternal 被调用: DeviceId={DeviceId}, DisplayName={DisplayName}, Reason={Reason}", 
                device.DeviceId, device.DisplayName, reason);
            
            // 检查是否已经选中相同设备（使用引用比较，支持未保存的设备）
            if (ReferenceEquals(_selectedDevice, device))
            {
                _logger.LogDebug("设备已选中（引用相同），设备ID: {DeviceId}", device.DeviceId);
                return true;
            }

            // 对于已保存的设备，检查ID是否相同
            // 注意：即使ID相同，如果引用不同（例如刷新后创建了新的ViewModel实例），
            // 也需要更新选择状态并触发事件，以确保UI绑定到正确的实例
            var isSameDeviceById = _selectedDevice != null && device.DeviceId > 0 && _selectedDevice.DeviceId == device.DeviceId;
            if (isSameDeviceById)
            {
                _logger.LogInformation("设备ID相同但引用不同，更新选择状态: DeviceId={DeviceId}", device.DeviceId);
            }
            
            var previousDevice = _selectedDevice;
            _selectedDevice = device;

            _logger.LogInformation("触发 SelectionChanged 事件: PreviousDevice={PreviousId}, CurrentDevice={CurrentId}", 
                previousDevice?.DeviceId, device.DeviceId);

            // 触发选择变更事件
            OnSelectionChanged(new DeviceSelectionChangedEventArgs
            {
                PreviousDevice = previousDevice,
                CurrentDevice = device,
                Reason = reason,
                IsValid = true
            });

            _logger.LogInformation("设备选择成功，设备ID: {DeviceId}, 显示名称: {DisplayName}",
                device.DeviceId, device.DisplayName);

            return true;
        }
    }

    /// <inheritdoc />
    public void ClearSelection()
    {
        lock (_lockObject)
        {
            var previousDevice = _selectedDevice;
            _selectedDevice = null;

            OnSelectionChanged(new DeviceSelectionChangedEventArgs
            {
                PreviousDevice = previousDevice,
                CurrentDevice = null,
                Reason = SelectionChangeReason.ClearSelection,
                IsValid = true
            });

            _logger.LogInformation("设备选择已清除");
        }
    }

    /// <inheritdoc />
    public bool IsSelectionValid()
    {
        lock (_lockObject)
        {
            if (_selectedDevice == null)
            {
                return true; // 无选择状态是有效的
            }

            var cachedDevice = _deviceCache.GetById(_selectedDevice.DeviceId);
            return cachedDevice != null;
        }
    }

    /// <summary>
    /// 触发选择变更事件
    /// </summary>
    private void OnSelectionChanged(DeviceSelectionChangedEventArgs args)
    {
        try
        {
            SelectionChanged?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发选择变更事件时发生错误");
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        // 简化版本无需释放资源
    }
}
