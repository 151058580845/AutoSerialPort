using System;
using System.Threading.Tasks;
using AutoSerialPort.Application.Abstractions;
using AutoSerialPort.Domain.Models;
using AutoSerialPort.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace AutoSerialPort.UI.ViewModels;

/// <summary>
/// 单设备配置视图模型，包含串口、解析与转发设置。
/// </summary>
public partial class DeviceProfileViewModel : ObservableObject, IDeviceProfileViewModel
{
    // 状态颜色 - 使用字符串避免线程问题
    private const string DisconnectedColor = "#94A3B8";
    private const string ConnectingColor = "#38BDF8";
    private const string ConnectedColor = "#22C55E";
    private const string ReconnectingColor = "#F59E0B";

    private readonly IAppController _appController;
    private readonly IUiApplyService _applyService;

    public SerialSettingsViewModel SerialSettings { get; }
    public ParserSettingsViewModel ParserSettings { get; }
    public ForwarderSettingsViewModel ForwarderSettings { get; }

    public IAsyncRelayCommand StartCommand { get; }
    public IAsyncRelayCommand StopCommand { get; }

    [ObservableProperty]
    private string _connectionStateText = "未连接";

    [ObservableProperty]
    private string _connectionStateColor = DisconnectedColor;

    [ObservableProperty]
    private string _identifierSummaryText = "识别: -";

    [ObservableProperty]
    private string _serialParameterSummaryText = "参数: -";

    [ObservableProperty]
    private string _portSummaryText = "连接: -";

    [ObservableProperty]
    private string _throughputText = "0.0 条/秒";

    [ObservableProperty]
    private string _forwardersText = "无";

    [ObservableProperty]
    private string _lastErrorText = string.Empty;

    [ObservableProperty]
    private bool _hasLastError;

    [ObservableProperty]
    private string _enabledText = "已启用";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isRunning;

    /// <summary>
    /// 设备ID - 实现IDeviceProfileViewModel接口
    /// </summary>
    public long DeviceId => SerialSettings.Config.Id;

    /// <summary>
    /// 显示名称 - 实现IDeviceProfileViewModel接口
    /// </summary>
    public string DisplayName => SerialSettings.Config.DisplayName ?? $"设备 {SerialSettings.Config.Id}";

    /// <summary>
    /// 是否已启用 - 实现IDeviceProfileViewModel接口
    /// </summary>
    public bool IsEnabled => SerialSettings.Config.IsEnabled;

    /// <summary>
    /// 串口设置 - 实现IDeviceProfileViewModel接口
    /// </summary>
    ISerialSettingsViewModel? IDeviceProfileViewModel.SerialSettings => SerialSettings;

    /// <summary>
    /// 解析器设置 - 实现IDeviceProfileViewModel接口
    /// </summary>
    IParserSettingsViewModel? IDeviceProfileViewModel.ParserSettings => ParserSettings;

    /// <summary>
    /// 转发器设置 - 实现IDeviceProfileViewModel接口
    /// </summary>
    IForwarderSettingsViewModel? IDeviceProfileViewModel.ForwarderSettings => ForwarderSettings;

    /// <summary>
    /// 创建设备视图模型。
    /// </summary>
    /// <param name="serialSettings">串口设置。</param>
    /// <param name="parserSettings">解析设置。</param>
    /// <param name="forwarderSettings">转发设置。</param>
    public DeviceProfileViewModel(
        SerialSettingsViewModel serialSettings,
        ParserSettingsViewModel parserSettings,
        ForwarderSettingsViewModel forwarderSettings,
        IAppController appController,
        IUiApplyService applyService)
    {
        SerialSettings = serialSettings;
        ParserSettings = parserSettings;
        ForwarderSettings = forwarderSettings;
        _appController = appController;
        _applyService = applyService;

        StartCommand = new AsyncRelayCommand(StartAsync, CanStart);
        StopCommand = new AsyncRelayCommand(StopAsync, CanStop);
    }

    /// <summary>
    /// 加载设备配置到子视图模型。
    /// </summary>
    /// <param name="profile">设备配置。</param>
    public void Load(SerialDeviceProfile profile)
    {
        SerialSettings.Load(profile.Serial);
        ParserSettings.Load(profile.Parser, profile.FrameDecoder);
        ForwarderSettings.Load(profile.Forwarders, profile.Serial.Id);
        RefreshSummary();
        UpdateStatus(null);
    }

    /// <summary>
    /// 从界面构建设备配置。
    /// </summary>
    public SerialDeviceProfile BuildProfile()
    {
        return new SerialDeviceProfile
        {
            Serial = SerialSettings.Config,
            Parser = ParserSettings.BuildConfig(),
            FrameDecoder = ParserSettings.BuildFrameDecoderConfig(),
            Forwarders = ForwarderSettings.BuildConfigs()
        };
    }

    /// <summary>
    /// 更新运行状态显示。
    /// </summary>
    /// <param name="status">设备状态快照。</param>
    public void UpdateStatus(SerialDeviceStatus? status)
    {
        if (status == null)
        {
            ConnectionStateText = "未启动";
            ConnectionStateColor = DisconnectedColor;
            PortSummaryText = "连接: -";
            ThroughputText = "0.0 条/秒";
            ForwardersText = "无";
            LastErrorText = string.Empty;
            HasLastError = false;
            IsConnected = false;
            IsRunning = false;
            RefreshSummary();
            return;
        }

        IsRunning = status.IsRunning;
        ConnectionStateText = status.IsRunning ? ToStateText(status.ConnectionState) : "已停止";
        ConnectionStateColor = status.IsRunning ? ToStateColor(status.ConnectionState) : DisconnectedColor;
        var portName = string.IsNullOrWhiteSpace(status.PortName) ? "-" : status.PortName;
        PortSummaryText = $"连接: {portName}";
        ThroughputText = $"{status.MessagesPerSecond:F1} 条/秒 (总计 {status.TotalMessages})";
        ForwardersText = status.ActiveForwarders.Length == 0 ? "无" : string.Join(", ", status.ActiveForwarders);
        LastErrorText = status.LastError ?? string.Empty;
        HasLastError = !string.IsNullOrWhiteSpace(LastErrorText);
        IsConnected = status.ConnectionState == SerialConnectionState.Connected;
        RefreshSummary();
    }

    /// <summary>
    /// 刷新概要信息显示。
    /// </summary>
    private void RefreshSummary()
    {
        var config = SerialSettings.Config;
        var identifierTypeText = ToIdentifierTypeText(config.IdentifierType);
        IdentifierSummaryText = $"识别: {identifierTypeText} | {config.IdentifierValue}";
        SerialParameterSummaryText = $"参数: {config.BaudRate} {config.DataBits}{ToParityShort(config.Parity)}{ToStopBitsShort(config.StopBits)}";
        EnabledText = config.IsEnabled ? "自动开启" : "手动开启";
    }

    partial void OnIsRunningChanged(bool value)
    {
        StartCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
    }

    private bool CanStart()
    {
        return SerialSettings.Config.Id > 0 && !IsRunning;
    }

    private bool CanStop()
    {
        return SerialSettings.Config.Id > 0 && IsRunning;
    }

    private async Task StartAsync()
    {
        await _applyService.ApplyAsync();

        if (SerialSettings.Config.Id <= 0)
        {
            LastErrorText = "请先保存/应用";
            HasLastError = true;
            return;
        }

        var result = await _appController.StartDeviceAsync(SerialSettings.Config.Id);
        if (!result.Success)
        {
            LastErrorText = result.Error ?? "启动失败";
            HasLastError = true;
        }
    }

    private async Task StopAsync()
    {
        if (SerialSettings.Config.Id <= 0)
        {
            LastErrorText = "请先保存/应用";
            HasLastError = true;
            return;
        }

        var result = await _appController.StopDeviceAsync(SerialSettings.Config.Id);
        if (!result.Success)
        {
            LastErrorText = result.Error ?? "停止失败";
            HasLastError = true;
            return;
        }

        IsRunning = false;
    }

    /// <summary>
    /// 将识别类型转换为显示文本。
    /// </summary>
    /// <param name="identifierType">识别类型。</param>
    private static string ToIdentifierTypeText(string? identifierType)
    {
        return identifierType?.Trim().ToLowerInvariant() switch
        {
            "byidpath" => "设备ID路径",
            "usbvidpid" => "USB VID/PID",
            "pnpdeviceid" => "PNP 设备ID",
            _ => "端口名称"
        };
    }

    /// <summary>
    /// 将校验位转换为简写。
    /// </summary>
    /// <param name="parity">校验位。</param>
    private static string ToParityShort(string? parity)
    {
        return parity?.Trim().ToLowerInvariant() switch
        {
            "odd" => "O",
            "even" => "E",
            "mark" => "M",
            "space" => "S",
            _ => "N"
        };
    }

    /// <summary>
    /// 将停止位转换为简写。
    /// </summary>
    /// <param name="stopBits">停止位。</param>
    private static string ToStopBitsShort(string? stopBits)
    {
        return stopBits?.Trim().ToLowerInvariant() switch
        {
            "two" => "2",
            "onepointfive" => "1.5",
            "none" => "0",
            _ => "1"
        };
    }

    /// <summary>
    /// 将连接状态转换为显示文本。
    /// </summary>
    /// <param name="state">连接状态。</param>
    private static string ToStateText(SerialConnectionState state)
    {
        return state switch
        {
            SerialConnectionState.Connected => "已连接",
            SerialConnectionState.Connecting => "连接中",
            SerialConnectionState.Reconnecting => "重连中",
            _ => "未连接"
        };
    }

    /// <summary>
    /// 将连接状态转换为显示颜色。
    /// </summary>
    /// <param name="state">连接状态。</param>
    private static string ToStateColor(SerialConnectionState state)
    {
        return state switch
        {
            SerialConnectionState.Connected => ConnectedColor,
            SerialConnectionState.Connecting => ConnectingColor,
            SerialConnectionState.Reconnecting => ReconnectingColor,
            _ => DisconnectedColor
        };
    }
}
