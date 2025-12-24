using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AutoSerialPort.Application.Abstractions;
using AutoSerialPort.Application.Models;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Models;

namespace AutoSerialPort.Application.Services;

/// <summary>
/// 设备数据管理器实现
/// 简化版本：直接从数据库加载数据，不使用缓存
/// </summary>
public class DeviceDataManager : IDeviceDataManager
{
    private readonly IConfigRepository _configRepository;
    private readonly IDeviceCache _deviceCache;
    private readonly ISelectionStateManager _selectionStateManager;
    private readonly Func<IDeviceProfileViewModel> _deviceViewModelFactory;
    private readonly ILogger<DeviceDataManager> _logger;

    private readonly ObservableCollection<IDeviceProfileViewModel> _devices;
    private volatile bool _isLoading;
    private long _currentProfileId;

    public DeviceDataManager(
        IConfigRepository configRepository,
        IDeviceCache deviceCache,
        ISelectionStateManager selectionStateManager,
        Func<IDeviceProfileViewModel> deviceViewModelFactory,
        ILogger<DeviceDataManager> logger)
    {
        _configRepository = configRepository ?? throw new ArgumentNullException(nameof(configRepository));
        _deviceCache = deviceCache ?? throw new ArgumentNullException(nameof(deviceCache));
        _selectionStateManager = selectionStateManager ?? throw new ArgumentNullException(nameof(selectionStateManager));
        _deviceViewModelFactory = deviceViewModelFactory ?? throw new ArgumentNullException(nameof(deviceViewModelFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _devices = new ObservableCollection<IDeviceProfileViewModel>();
    }

    /// <inheritdoc />
    public ObservableCollection<IDeviceProfileViewModel> Devices => _devices;

    /// <inheritdoc />
    public bool IsLoading => _isLoading;

    /// <inheritdoc />
    public bool IsCacheValid => _devices.Count > 0;

    /// <inheritdoc />
    public bool ShouldBackgroundRefresh => false;

    /// <inheritdoc />
    public event EventHandler<DataLoadingEventArgs>? LoadingStateChanged;

    /// <inheritdoc />
    public async Task PreloadDataAsync(CancellationToken cancellationToken = default)
    {
        await LoadDataInternalAsync("预加载", cancellationToken);
    }

    /// <inheritdoc />
    public async Task RefreshDataAsync(bool preserveSelection = true, CancellationToken cancellationToken = default)
    {
        IDeviceProfileViewModel? previousSelection = null;
        long previousDeviceId = 0;
        string? previousDisplayName = null;
        
        if (preserveSelection)
        {
            previousSelection = _selectionStateManager.SelectedDevice;
            if (previousSelection != null)
            {
                previousDeviceId = previousSelection.DeviceId;
                previousDisplayName = previousSelection.DisplayName;
                _logger.LogInformation("保存当前选择状态: DeviceId={DeviceId}, DisplayName={DisplayName}", 
                    previousDeviceId, previousDisplayName);
            }
            else
            {
                _logger.LogInformation("当前没有选中的设备");
            }
        }

        await LoadDataInternalAsync("刷新", cancellationToken);

        // 恢复选择状态
        if (preserveSelection && previousDeviceId > 0)
        {
            _logger.LogInformation("尝试恢复选择状态: DeviceId={DeviceId}, DisplayName={DisplayName}", 
                previousDeviceId, previousDisplayName);
            
            var device = _deviceCache.GetById(previousDeviceId);
            if (device == null && !string.IsNullOrEmpty(previousDisplayName))
            {
                _logger.LogDebug("通过ID未找到设备，尝试通过显示名称查找");
                device = _deviceCache.GetByDisplayName(previousDisplayName);
            }
            
            if (device != null)
            {
                _logger.LogInformation("找到匹配设备，恢复选择: DeviceId={DeviceId}, DisplayName={DisplayName}", 
                    device.DeviceId, device.DisplayName);
                await _selectionStateManager.SelectDeviceAsync(device.DeviceId, cancellationToken);
            }
            else
            {
                _logger.LogWarning("未找到匹配设备，选择第一个可用设备");
                if (_devices.Count > 0)
                {
                    await _selectionStateManager.SelectDeviceAsync(_devices[0].DeviceId, cancellationToken);
                }
            }
        }
        else if (preserveSelection && !string.IsNullOrEmpty(previousDisplayName))
        {
            // 对于未保存的设备（ID为0），尝试通过显示名称匹配
            _logger.LogInformation("尝试通过显示名称恢复选择: DisplayName={DisplayName}", previousDisplayName);
            var device = _deviceCache.GetByDisplayName(previousDisplayName);
            if (device != null)
            {
                _logger.LogInformation("通过显示名称找到设备，恢复选择: DeviceId={DeviceId}", device.DeviceId);
                await _selectionStateManager.SelectDeviceAsync(device.DeviceId, cancellationToken);
            }
            else if (_devices.Count > 0)
            {
                _logger.LogWarning("未找到匹配设备，选择第一个可用设备");
                await _selectionStateManager.SelectDeviceAsync(_devices[0].DeviceId, cancellationToken);
            }
        }
    }

    /// <summary>
    /// 内部数据加载方法
    /// </summary>
    private async Task LoadDataInternalAsync(string operation, CancellationToken cancellationToken)
    {
        if (_isLoading)
        {
            _logger.LogDebug("数据正在加载中，跳过重复请求");
            return;
        }

        try
        {
            _isLoading = true;
            SetLoadingState(true, $"{operation}设备数据");
            _logger.LogInformation("开始{Operation}设备数据", operation);

            // 确保默认方案存在并获取当前方案ID
            _currentProfileId = await _configRepository.EnsureDefaultProfileAsync();

            // 从数据库加载设备配置
            var profiles = await _configRepository.GetSerialDeviceProfilesAsync(_currentProfileId);
            _logger.LogDebug("从数据库加载了 {Count} 个设备配置", profiles?.Length ?? 0);

            // 转换为ViewModel
            var deviceViewModels = ConvertToViewModels(profiles ?? Array.Empty<SerialDeviceProfile>());

            // 更新缓存
            _deviceCache.UpdateCache(deviceViewModels);

            // 更新UI集合
            _devices.Clear();
            foreach (var device in deviceViewModels)
            {
                _devices.Add(device);
            }

            SetLoadingState(false, $"{operation}完成", deviceCount: deviceViewModels.Count);
            _logger.LogInformation("设备数据{Operation}完成，共加载 {Count} 个设备", operation, deviceViewModels.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Operation}设备数据时发生错误", operation);
            SetLoadingState(false, $"{operation}失败", ex);
            throw;
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <inheritdoc />
    public Task<IDeviceProfileViewModel?> GetDeviceAsync(long deviceId)
    {
        if (deviceId <= 0)
        {
            return Task.FromResult<IDeviceProfileViewModel?>(null);
        }

        var device = _deviceCache.GetById(deviceId);
        return Task.FromResult(device);
    }

    /// <inheritdoc />
    public Task<IDeviceProfileViewModel?> GetDeviceAsync(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Task.FromResult<IDeviceProfileViewModel?>(null);
        }

        var device = _deviceCache.GetByDisplayName(displayName);
        return Task.FromResult(device);
    }

    /// <summary>
    /// 将SerialDeviceProfile转换为IDeviceProfileViewModel
    /// </summary>
    private IReadOnlyList<IDeviceProfileViewModel> ConvertToViewModels(SerialDeviceProfile[] profiles)
    {
        var viewModels = new List<IDeviceProfileViewModel>();

        for (int i = 0; i < profiles.Length; i++)
        {
            try
            {
                var viewModel = _deviceViewModelFactory();

                // 如果配置中的ID无效，使用索引+1
                if (profiles[i].Serial.Id <= 0)
                {
                    profiles[i].Serial.Id = i + 1;
                }

                viewModel.Load(profiles[i]);
                viewModels.Add(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "转换设备配置到ViewModel时发生错误，索引: {Index}", i);
            }
        }

        return viewModels;
    }

    /// <summary>
    /// 设置加载状态并触发事件
    /// </summary>
    private void SetLoadingState(bool isLoading, string? operation = null, Exception? error = null, int deviceCount = 0)
    {
        var args = new DataLoadingEventArgs
        {
            IsLoading = isLoading,
            Operation = operation,
            Error = error,
            DeviceCount = deviceCount,
            Progress = 0,
            ShowProgress = false
        };

        try
        {
            LoadingStateChanged?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发加载状态变更事件时发生错误");
        }
    }

    /// <summary>
    /// 智能刷新数据（简化版本，直接调用刷新）
    /// </summary>
    public Task SmartRefreshAsync(bool forceRefresh = false, bool preserveSelection = true, CancellationToken cancellationToken = default)
    {
        return RefreshDataAsync(preserveSelection, cancellationToken);
    }

    /// <summary>
    /// 启用或禁用后台刷新（保留接口兼容，无实际操作）
    /// </summary>
    public void SetBackgroundRefreshEnabled(bool enabled)
    {
        // 简化版本不使用后台刷新
    }

    /// <summary>
    /// 获取统计信息
    /// </summary>
    public DataManagerStatistics GetStatistics()
    {
        return new DataManagerStatistics
        {
            DeviceCount = _devices.Count,
            CacheDeviceCount = _deviceCache.GetAll().Count,
            IsLoading = _isLoading,
            IsCacheValid = true,
            LastCacheUpdate = DateTime.UtcNow,
            LastDataRefresh = DateTime.UtcNow,
            ShouldBackgroundRefresh = false,
            IsBackgroundRefreshEnabled = false
        };
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        // 简化版本无需释放资源
    }
}

/// <summary>
/// 数据管理器统计信息
/// </summary>
public class DataManagerStatistics
{
    public int DeviceCount { get; init; }
    public int CacheDeviceCount { get; init; }
    public bool IsLoading { get; init; }
    public bool IsCacheValid { get; init; }
    public DateTime LastCacheUpdate { get; init; }
    public DateTime LastDataRefresh { get; init; }
    public bool ShouldBackgroundRefresh { get; init; }
    public bool IsBackgroundRefreshEnabled { get; init; }

    public override string ToString()
    {
        return $"DeviceCount: {DeviceCount}, IsLoading: {IsLoading}";
    }
}
