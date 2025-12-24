using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoSerialPort.Application.Abstractions;
using AutoSerialPort.Application.Models;
using AutoSerialPort.Domain.Entities;
using AutoSerialPort.Domain.Models;
using AutoSerialPort.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;

namespace AutoSerialPort.UI.ViewModels;

/// <summary>
/// 主窗口视图模型，负责设备列表与全局操作。
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly IAppController _appController;
    private readonly IDeviceDataManager _deviceDataManager;
    private readonly ISelectionStateManager _selectionStateManager;
    private readonly Func<DeviceProfileViewModel> _deviceProfileFactory;
    private readonly ILogger<MainWindowViewModel> _logger;
    private AppState _state = new();
    private readonly DispatcherTimer _timer;

    [ObservableProperty]
    private IDeviceProfileViewModel? _selectedDevice;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _loadingMessage;

    [ObservableProperty]
    private bool _showEmptyState;

    [ObservableProperty]
    private string _emptyStateTitle = "暂无设备";

    [ObservableProperty]
    private string _emptyStateDescription = "当前没有配置任何串口设备，点击下方按钮添加第一个设备";

    [ObservableProperty]
    private double _loadingProgress;

    [ObservableProperty]
    private bool _showLoadingProgress;

    /// <summary>
    /// 获取当前是否正在进行数据操作
    /// </summary>
    public bool IsBusy => IsLoading || _deviceDataManager.IsLoading;

    public ObservableCollection<IDeviceProfileViewModel> Devices => _deviceDataManager.Devices;

    public LogViewModel LogViewModel { get; }
    public StatusBarViewModel StatusBar { get; }
    public SerialConsoleViewModel SerialConsole { get; }

    public IAsyncRelayCommand ApplyCommand { get; }
    public IAsyncRelayCommand StartCommand { get; }
    public IAsyncRelayCommand StopCommand { get; }
    public IRelayCommand AddDeviceCommand { get; }
    public IRelayCommand RemoveDeviceCommand { get; }
    public IAsyncRelayCommand<IDeviceProfileViewModel> SelectDeviceCommand { get; }
    public IAsyncRelayCommand RefreshDevicesCommand { get; }

    /// <summary>
    /// 创建主窗口视图模型。
    /// </summary>
    /// <param name="appController">应用控制器。</param>
    /// <param name="deviceDataManager">设备数据管理器。</param>
    /// <param name="selectionStateManager">设备选择状态管理器。</param>
    /// <param name="deviceProfileFactory">设备视图模型工厂。</param>
    /// <param name="logViewModel">日志视图模型。</param>
    /// <param name="statusBar">状态栏视图模型。</param>
    /// <param name="serialConsole">串口收发视图模型。</param>
    /// <param name="applyService">UI应用服务。</param>
    /// <param name="logger">日志记录器。</param>
    public MainWindowViewModel(
        IAppController appController,
        IDeviceDataManager deviceDataManager,
        ISelectionStateManager selectionStateManager,
        Func<DeviceProfileViewModel> deviceProfileFactory,
        LogViewModel logViewModel,
        StatusBarViewModel statusBar,
        SerialConsoleViewModel serialConsole,
        IUiApplyService applyService,
        ILogger<MainWindowViewModel> logger)
    {
        _appController = appController;
        _deviceDataManager = deviceDataManager ?? throw new ArgumentNullException(nameof(deviceDataManager));
        _selectionStateManager = selectionStateManager ?? throw new ArgumentNullException(nameof(selectionStateManager));
        _deviceProfileFactory = deviceProfileFactory;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        LogViewModel = logViewModel;
        StatusBar = statusBar;
        SerialConsole = serialConsole;
        applyService.Register(ApplyAsync);

        ApplyCommand = new AsyncRelayCommand(ApplyAsync);
        StartCommand = new AsyncRelayCommand(StartAsync);
        StopCommand = new AsyncRelayCommand(StopAsync);
        AddDeviceCommand = new RelayCommand(AddDevice);
        RemoveDeviceCommand = new RelayCommand(RemoveSelectedDevice);
        SelectDeviceCommand = new AsyncRelayCommand<IDeviceProfileViewModel>(SelectDeviceAsync);
        RefreshDevicesCommand = new AsyncRelayCommand(RefreshDevicesAsync);

        // 订阅设备选择状态变更事件
        _selectionStateManager.SelectionChanged += OnSelectionStateChanged;
        
        // 订阅数据加载状态变更事件
        _deviceDataManager.LoadingStateChanged += OnDataLoadingStateChanged;

        // 订阅设备集合变更事件以更新空状态显示
        _deviceDataManager.Devices.CollectionChanged += OnDevicesCollectionChanged;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => UpdateStatus();
        _timer.Start();
    }

    /// <summary>
    /// 初始化界面数据（简化版本，直接加载）。
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("开始初始化主窗口数据");

            _state = await _appController.LoadAsync();

            // 直接从数据库加载设备数据
            await _deviceDataManager.PreloadDataAsync();

            // 如果没有设备，添加一个默认设备
            if (_deviceDataManager.Devices.Count == 0)
            {
                AddDevice();
            }
            else
            {
                // 选择第一个设备
                var firstDevice = _deviceDataManager.Devices.FirstOrDefault();
                if (firstDevice != null)
                {
                    await _selectionStateManager.SelectDeviceAsync(firstDevice.DeviceId);
                }
            }

            UpdateStatus();
            _logger.LogInformation("主窗口数据初始化完成，设备数量: {DeviceCount}", _deviceDataManager.Devices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化主窗口数据时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 使用预加载数据初始化界面数据（简化版本，与InitializeAsync相同）
    /// </summary>
    public Task InitializeWithPreloadedDataAsync()
    {
        return InitializeAsync();
    }

    /// <summary>
    /// 保存并应用配置。
    /// </summary>
    private async Task ApplyAsync()
    {
        try
        {
            _logger.LogInformation("开始应用配置");
            // 汇总当前 UI 配置
            _state.Devices = _deviceDataManager.Devices
                .OfType<DeviceProfileViewModel>()
                .Select(x => x.BuildProfile())
                .ToArray();
                
            await _appController.ApplyAsync(_state);
            _state = await _appController.LoadAsync();
            
            // 使用新的数据管理器刷新数据，保持选择状态
            await _deviceDataManager.RefreshDataAsync(preserveSelection: true);
            
            UpdateStatus();
            _logger.LogInformation("配置应用完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "应用配置时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 启动采集。
    /// </summary>
    private async Task StartAsync()
    {
        await ApplyAsync();
        await _appController.StartAsync();
    }

    /// <summary>
    /// 停止采集。
    /// </summary>
    private Task StopAsync() => _appController.StopAsync();

    /// <summary>
    /// 刷新状态栏与设备状态。
    /// </summary>
    private void UpdateStatus()
    {
        try
        {
            var snapshot = _appController.GetStatus();
            StatusBar.Update(snapshot);
            var statusMap = snapshot.DeviceStatuses.ToDictionary(x => x.DeviceId);
            
            foreach (var device in _deviceDataManager.Devices.OfType<DeviceProfileViewModel>())
            {
                if (device.DeviceId > 0 && statusMap.TryGetValue(device.DeviceId, out var status))
                {
                    device.UpdateStatus(status);
                }
                else
                {
                    device.UpdateStatus(null);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新状态时发生错误");
        }
    }

    /// <summary>
    /// 添加一条新设备配置。
    /// </summary>
    private void AddDevice()
    {
        try
        {
            _logger.LogDebug("添加新设备配置");
            
            var displayName = $"设备{_deviceDataManager.Devices.Count + 1}";
            var profile = new SerialDeviceProfile
            {
                Serial = CreateDefaultDeviceConfig(displayName),
                Parser = new ParserConfig
                {
                    ParserType = "LineParser",
                    ParametersJson = "{\"encoding\":\"utf-8\",\"separator\":\"\\n\"}"
                },
                FrameDecoder = new FrameDecoderConfig
                {
                    DecoderType = "DelimiterFrameDecoder",
                    ParametersJson = "{\"encoding\":\"utf-8\",\"delimiter\":\"\\n\",\"includeDelimiter\":true,\"maxBufferLength\":65536}"
                },
                Forwarders = CreateDefaultForwarders(9000 + _deviceDataManager.Devices.Count)
            };

            // 使用工厂创建ViewModel并初始化
            var vm = _deviceProfileFactory();
            vm.Load(profile);
            
            // 如果是UI层的DeviceProfileViewModel，初始化SerialSettings
            if (vm is UI.ViewModels.DeviceProfileViewModel uiVm)
            {
                _ = uiVm.SerialSettings.InitializeAsync();
            }
            
            // 直接添加到数据管理器的集合中
            _deviceDataManager.Devices.Add(vm);
            
            // 选择新添加的设备（未保存时设备ID可能为0，跳过选择同步）
            if (vm.DeviceId > 0)
            {
                _ = Task.Run(async () => await _selectionStateManager.SelectDeviceAsync(vm.DeviceId));
            }
            
            _logger.LogInformation("已添加新设备配置: {DisplayName}", displayName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加设备配置时发生错误");
        }
    }

    /// <summary>
    /// 构建默认转发器集合。
    /// </summary>
    /// <param name="port">TCP 端口。</param>
    private static ForwarderConfig[] CreateDefaultForwarders(int port)
    {
        return new[]
        {
            new ForwarderConfig
            {
                ForwarderType = "TcpForwarder",
                IsEnabled = false,
                ParametersJson = $"{{\"mode\":\"Server\",\"host\":\"0.0.0.0\",\"port\":{port}}}"
            },
            new ForwarderConfig
            {
                ForwarderType = "MqttForwarder",
                IsEnabled = false,
                ParametersJson = "{\"broker\":\"localhost\",\"port\":1883,\"topic\":\"demo/topic\",\"qos\":0,\"retain\":false}"
            },
            new ForwarderConfig
            {
                ForwarderType = "ClipboardForwarder",
                IsEnabled = false,
                ParametersJson = "{\"appendNewLine\":false}"
            },
            new ForwarderConfig
            {
                ForwarderType = "TypingForwarder",
                IsEnabled = false,
                ParametersJson = "{\"delayMs\":0}"
            }
        };
    }

    /// <summary>
    /// 移除当前选中的设备。
    /// </summary>
    private void RemoveSelectedDevice()
    {
        try
        {
            if (SelectedDevice == null)
            {
                _logger.LogDebug("没有选中的设备，无法移除");
                return;
            }

            _logger.LogDebug("移除选中的设备: {DisplayName}", SelectedDevice.DisplayName);
            
            var deviceToRemove = SelectedDevice;
            _deviceDataManager.Devices.Remove(deviceToRemove);
            
            // 选择第一个可用设备或清除选择
            var firstDevice = _deviceDataManager.Devices.FirstOrDefault();
            if (firstDevice != null)
            {
                _ = Task.Run(async () => await _selectionStateManager.SelectDeviceAsync(firstDevice.DeviceId));
            }
            else
            {
                _selectionStateManager.ClearSelection();
            }
            
            _logger.LogInformation("已移除设备: {DisplayName}", deviceToRemove.DisplayName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除设备时发生错误");
        }
    }

    /// <summary>
    /// 创建默认串口配置。
    /// </summary>
    private static SerialDeviceConfig CreateDefaultDeviceConfig()
    {
        var defaultPort = OperatingSystem.IsWindows() ? "COM3" : "/dev/ttyUSB0";
        return new SerialDeviceConfig
        {
            DisplayName = "设备1",
            IdentifierType = SerialIdentifierTypes.PortName,
            IdentifierValue = defaultPort,
            BaudRate = 9600,
            Parity = "None",
            DataBits = 8,
            StopBits = "One",
            IsEnabled = true
        };
    }

    /// <summary>
    /// 创建带显示名称的默认串口配置。
    /// </summary>
    /// <param name="displayName">显示名称。</param>
    private static SerialDeviceConfig CreateDefaultDeviceConfig(string displayName)
    {
        var config = CreateDefaultDeviceConfig();
        config.DisplayName = displayName;
        return config;
    }

    /// <summary>
    /// 异步选择设备
    /// </summary>
    /// <param name="device">要选择的设备</param>
    private async Task SelectDeviceAsync(IDeviceProfileViewModel? device)
    {
        if (IsBusy)
        {
            _logger.LogDebug("系统忙碌中，跳过设备选择操作");
            return;
        }

        if (device == null)
        {
            _logger.LogDebug("尝试选择空设备，清除选择状态");
            _selectionStateManager.ClearSelection();
            return;
        }

        try
        {
            _logger.LogDebug("用户选择设备: {DisplayName} (ID: {DeviceId})", device.DisplayName, device.DeviceId);
            
            var success = await _selectionStateManager.SelectDeviceAsync(device.DeviceId);
            if (!success)
            {
                _logger.LogWarning("设备选择失败: {DisplayName} (ID: {DeviceId})", device.DisplayName, device.DeviceId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "选择设备时发生错误: {DisplayName} (ID: {DeviceId})", device.DisplayName, device.DeviceId);
        }
    }

    /// <summary>
    /// 刷新设备数据
    /// </summary>
    public async Task RefreshDevicesAsync()
    {
        if (IsBusy)
        {
            _logger.LogDebug("系统忙碌中，跳过设备刷新操作");
            return;
        }

        try
        {
            _logger.LogInformation("手动刷新设备数据");
            await _deviceDataManager.RefreshDataAsync(preserveSelection: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新设备数据时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 处理设备选择状态变更事件
    /// </summary>
    private void OnSelectionStateChanged(object? sender, DeviceSelectionChangedEventArgs e)
    {
        try
        {
            _logger.LogInformation("OnSelectionStateChanged 被调用: CurrentDevice={DeviceId}, Reason={Reason}", 
                e.CurrentDevice?.DeviceId, e.Reason);
            
            // 在UI线程上更新选中设备
            Dispatcher.UIThread.Post(() =>
            {
                _logger.LogDebug("UI线程开始处理选择变更");
                
                var targetDevice = e.CurrentDevice;
                if (targetDevice != null)
                {
                    // 首先尝试在设备列表中找到匹配的设备（使用引用比较或ID比较）
                    var matched = _deviceDataManager.Devices.FirstOrDefault(d =>
                        ReferenceEquals(d, targetDevice) ||
                        (targetDevice.DeviceId > 0 && d.DeviceId == targetDevice.DeviceId));

                    if (matched == null)
                    {
                        _logger.LogWarning("选择状态中的设备未出现在当前列表中，忽略更新: {DisplayName} (ID: {DeviceId})",
                            targetDevice.DisplayName, targetDevice.DeviceId);
                        return;
                    }

                    _logger.LogDebug("在设备列表中找到匹配设备: DeviceId={DeviceId}, 引用相同={IsSameRef}", 
                        matched.DeviceId, ReferenceEquals(matched, targetDevice));
                    targetDevice = matched;
                }

                // 只有当选择真正发生变化时才更新UI
                var currentSelectedId = SelectedDevice?.DeviceId;
                var targetId = targetDevice?.DeviceId;
                _logger.LogDebug("比较选择状态: 当前SelectedDevice.Id={CurrentId}, 目标Device.Id={TargetId}, 引用相同={IsSameRef}", 
                    currentSelectedId, targetId, ReferenceEquals(SelectedDevice, targetDevice));
                
                if (!ReferenceEquals(SelectedDevice, targetDevice))
                {
                    _logger.LogInformation("更新 SelectedDevice: 从 {OldId} 到 {NewId}", 
                        SelectedDevice?.DeviceId, targetDevice?.DeviceId);
                    SelectedDevice = targetDevice;

                    // 验证选择状态的有效性
                    if (!e.IsValid && targetDevice != null)
                    {
                        _logger.LogWarning("检测到无效的设备选择状态: {DisplayName} (ID: {DeviceId})",
                            targetDevice.DisplayName, targetDevice.DeviceId);
                    }
                }
                else
                {
                    _logger.LogDebug("SelectedDevice 引用相同，跳过更新");
                }

                _logger.LogDebug("设备选择状态已更新: {DisplayName}, 原因: {Reason}, 有效: {IsValid}",
                    targetDevice?.DisplayName ?? "无", e.Reason, e.IsValid);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理设备选择状态变更时发生错误");
        }
    }

    /// <summary>
    /// 处理数据加载状态变更事件
    /// </summary>
    private void OnDataLoadingStateChanged(object? sender, DataLoadingEventArgs e)
    {
        try
        {
            // 在UI线程上更新加载状态
            Dispatcher.UIThread.Post(() =>
            {
                IsLoading = e.IsLoading;
                LoadingMessage = e.Operation;
                LoadingProgress = e.Progress;
                ShowLoadingProgress = e.ShowProgress;
                OnPropertyChanged(nameof(IsBusy)); // 通知IsBusy属性变更
                
                // 更新空状态显示
                UpdateEmptyState();
            });
            
            if (e.IsLoading)
            {
                _logger.LogDebug("数据加载开始: {Operation}", e.Operation);
            }
            else
            {
                if (e.Error != null)
                {
                    _logger.LogError(e.Error, "数据加载失败: {Operation}", e.Operation);
                }
                else
                {
                    _logger.LogDebug("数据加载完成: {Operation}, 设备数量: {DeviceCount}", e.Operation, e.DeviceCount);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理数据加载状态变更时发生错误");
        }
    }

    /// <summary>
    /// 处理设备集合变更事件
    /// </summary>
    private void OnDevicesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        try
        {
            // 在UI线程上更新空状态显示
            Dispatcher.UIThread.Post(() =>
            {
                UpdateEmptyState();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理设备集合变更时发生错误");
        }
    }

    /// <summary>
    /// 更新空状态显示
    /// </summary>
    private void UpdateEmptyState()
    {
        try
        {
            // 只有在不加载且设备列表为空时才显示空状态
            var shouldShowEmpty = !IsBusy && _deviceDataManager.Devices.Count == 0;
            
            if (ShowEmptyState != shouldShowEmpty)
            {
                ShowEmptyState = shouldShowEmpty;
                
                if (shouldShowEmpty)
                {
                    _logger.LogDebug("显示空状态指示器");
                }
                else
                {
                    _logger.LogDebug("隐藏空状态指示器");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新空状态显示时发生错误");
        }
    }

    /// <summary>
    /// 处理设备选择状态变更事件
    /// </summary>

    partial void OnSelectedDeviceChanged(IDeviceProfileViewModel? oldValue, IDeviceProfileViewModel? newValue)
    {
        try
        {
            SerialConsole.SetActiveDevice(newValue as DeviceProfileViewModel);

            // 如果新选中的设备与旧设备不同，同步到 SelectionStateManager
            if (newValue != null && newValue != oldValue)
            {
                // 异步更新选择状态管理器（使用设备对象直接选择，支持未保存的设备）
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _selectionStateManager.SelectDeviceAsync(newValue);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "同步设备选择状态时发生错误");
                    }
                });
            }

            _logger.LogDebug("串口控制台活动设备已更新: {DisplayName}", newValue?.DisplayName ?? "无");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新串口控制台活动设备时发生错误");
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        try
        {
            // 取消订阅事件
            if (_selectionStateManager != null)
            {
                _selectionStateManager.SelectionChanged -= OnSelectionStateChanged;
            }
            
            if (_deviceDataManager != null)
            {
                _deviceDataManager.LoadingStateChanged -= OnDataLoadingStateChanged;
                _deviceDataManager.Devices.CollectionChanged -= OnDevicesCollectionChanged;
            }
            
            _timer?.Stop();
            _logger.LogDebug("MainWindowViewModel资源已释放");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放MainWindowViewModel资源时发生错误");
        }
    }
}
