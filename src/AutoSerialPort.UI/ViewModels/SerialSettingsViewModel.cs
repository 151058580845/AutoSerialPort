using System.Collections.ObjectModel;
using System.Windows.Input;
using AutoSerialPort.Application.Abstractions;
using AutoSerialPort.Application.Models;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Entities;
using AutoSerialPort.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoSerialPort.UI.ViewModels;

/// <summary>
/// 串口设置视图模型，负责端口扫描与参数绑定。
/// </summary>
public partial class SerialSettingsViewModel : ObservableObject, ISerialSettingsViewModel
{
    private readonly ISerialPortDiscoveryService _discoveryService;

    [ObservableProperty]
    private SerialDeviceConfig _config = new();

    [ObservableProperty]
    private SerialPortDescriptor? _selectedPort;

    [ObservableProperty]
    private Application.Models.OptionItem? _selectedParityOption;

    [ObservableProperty]
    private Application.Models.OptionItem? _selectedStopBitsOption;

    [ObservableProperty]
    private Application.Models.OptionItem? _selectedIdentifierTypeOption;

    [ObservableProperty]
    private bool _isShowAdvancedSettings;
    public ObservableCollection<SerialPortDescriptor> Ports { get; } = new();

    public ObservableCollection<Application.Models.OptionItem> ParityOptions { get; } = new(new[]
    {
        new Application.Models.OptionItem("None", "无校验"),
        new Application.Models.OptionItem("Odd", "奇校验"),
        new Application.Models.OptionItem("Even", "偶校验"),
        new Application.Models.OptionItem("Mark", "标记校验"),
        new Application.Models.OptionItem("Space", "空格校验")
    });

    public ObservableCollection<Application.Models.OptionItem> StopBitsOptions { get; } = new(new[]
    {
        new Application.Models.OptionItem("One", "1位"),
        new Application.Models.OptionItem("Two", "2位"),
        new Application.Models.OptionItem("OnePointFive", "1.5位"),
        new Application.Models.OptionItem("None", "无")
    });

    public ObservableCollection<Application.Models.OptionItem> IdentifierTypeOptions { get; } = new(new[]
    {
        new Application.Models.OptionItem(SerialIdentifierTypes.PortName, "端口名称"),
        new Application.Models.OptionItem(SerialIdentifierTypes.ByIdPath, "设备ID路径(/dev/serial/by-id)"),
        new Application.Models.OptionItem(SerialIdentifierTypes.UsbVidPid, "USB VID/PID"),
        new Application.Models.OptionItem(SerialIdentifierTypes.PnpDeviceId, "PNP 设备ID")
    });

    public IAsyncRelayCommand RefreshPortsCommand { get; }

    public IRelayCommand ApplySelectedPortCommand { get; }
    
    public IRelayCommand ShowAdvancedSettingsCommand { get; }

    /// <summary>
    /// 刷新端口命令 - 实现接口
    /// </summary>
    ICommand ISerialSettingsViewModel.RefreshPortsCommand => RefreshPortsCommand;

    /// <summary>
    /// 显示高级设置命令 - 实现接口
    /// </summary>
    ICommand ISerialSettingsViewModel.ShowAdvancedSettingsCommand => ShowAdvancedSettingsCommand;

    /// <summary>
    /// 创建串口设置视图模型。
    /// </summary>
    /// <param name="discoveryService">串口扫描服务。</param>
    public SerialSettingsViewModel(ISerialPortDiscoveryService discoveryService)
    {
        _discoveryService = discoveryService;
        RefreshPortsCommand = new AsyncRelayCommand(RefreshPortsAsync);
        ApplySelectedPortCommand = new RelayCommand(ApplySelectedPort);
        ShowAdvancedSettingsCommand = new RelayCommand(ShowAdvancedSettings);
    }

    /// <summary>
    /// 初始化端口列表。
    /// </summary>
    public async Task InitializeAsync()
    {
        await RefreshPortsAsync();
    }

    /// <summary>
    /// 加载配置到界面。
    /// </summary>
    /// <param name="config">串口配置。</param>
    public void Load(SerialDeviceConfig config)
    {
        Config = config;
        if (string.IsNullOrWhiteSpace(Config.IdentifierType))
        {
            Config.IdentifierType = SerialIdentifierTypes.PortName;
        }

        SelectedParityOption = ParityOptions.FirstOrDefault(x =>
            string.Equals(x.Value, Config.Parity, StringComparison.OrdinalIgnoreCase)) ?? ParityOptions.FirstOrDefault();
        SelectedStopBitsOption = StopBitsOptions.FirstOrDefault(x =>
            string.Equals(x.Value, Config.StopBits, StringComparison.OrdinalIgnoreCase)) ?? StopBitsOptions.FirstOrDefault();
        SelectedIdentifierTypeOption = IdentifierTypeOptions.FirstOrDefault(x =>
            string.Equals(x.Value, Config.IdentifierType, StringComparison.OrdinalIgnoreCase)) ?? IdentifierTypeOptions.FirstOrDefault();
    }

    /// <summary>
    /// 刷新可用串口列表。
    /// </summary>
    private async Task RefreshPortsAsync()
    {
        Ports.Clear();
        var ports = await _discoveryService.GetAvailablePortsAsync(CancellationToken.None);
        foreach (var port in ports)
        {
            Ports.Add(port);
        }

        // 尝试匹配当前配置对应的端口
        SelectedPort = Ports.FirstOrDefault(x => IsMatch(x, Config.IdentifierType, Config.IdentifierValue));

        if (Ports.Count == 0)
        {
            // 无可用端口时，保持当前配置的占位显示
            Ports.Add(new SerialPortDescriptor { PortName = Config.IdentifierValue, DisplayName = Config.IdentifierValue });
        }
    }

    private void ShowAdvancedSettings()
    {
        IsShowAdvancedSettings = !IsShowAdvancedSettings;
    }

    /// <summary>
    /// 将当前选中端口写回配置。
    /// </summary>
    private void ApplySelectedPort()
    {
        if (SelectedPort == null)
        {
            return;
        }

        var type = Config.IdentifierType;
        if (string.Equals(type, SerialIdentifierTypes.ByIdPath, StringComparison.OrdinalIgnoreCase))
        {
            Config.IdentifierValue = SelectedPort.ByIdPath ?? SelectedPort.PortName;
            return;
        }

        if (string.Equals(type, SerialIdentifierTypes.UsbVidPid, StringComparison.OrdinalIgnoreCase))
        {
            Config.IdentifierValue = SelectedPort.VidPid ?? SelectedPort.PortName;
            return;
        }

        if (string.Equals(type, SerialIdentifierTypes.PnpDeviceId, StringComparison.OrdinalIgnoreCase))
        {
            Config.IdentifierValue = SelectedPort.PnpDeviceId ?? SelectedPort.PortName;
            return;
        }

        Config.IdentifierValue = SelectedPort.PortName;
    }

    /// <summary>
    /// 处理校验位选项变更。
    /// </summary>
    partial void OnSelectedParityOptionChanged(Application.Models.OptionItem? value)
    {
        if (value != null)
        {
            Config.Parity = value.Value;
        }
    }

    /// <summary>
    /// 处理停止位选项变更。
    /// </summary>
    partial void OnSelectedStopBitsOptionChanged(Application.Models.OptionItem? value)
    {
        if (value != null)
        {
            Config.StopBits = value.Value;
        }
    }

    /// <summary>
    /// 处理识别方式变更。
    /// </summary>
    partial void OnSelectedIdentifierTypeOptionChanged(Application.Models.OptionItem? value)
    {
        if (value != null)
        {
            Config.IdentifierType = value.Value;
            ApplySelectedPort();
        }
    }

    /// <summary>
    /// 处理端口选择变更。
    /// </summary>
    partial void OnSelectedPortChanged(SerialPortDescriptor? value)
    {
        if (value != null)
        {
            ApplySelectedPort();
        }
    }

    /// <summary>
    /// 判断端口是否匹配当前识别配置。
    /// </summary>
    private static bool IsMatch(SerialPortDescriptor port, string? identifierType, string? identifierValue)
    {
        if (string.IsNullOrWhiteSpace(identifierValue))
        {
            return false;
        }

        if (string.Equals(identifierType, SerialIdentifierTypes.ByIdPath, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(port.ByIdPath, identifierValue, StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(identifierType, SerialIdentifierTypes.UsbVidPid, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(port.VidPid, identifierValue, StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(identifierType, SerialIdentifierTypes.PnpDeviceId, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(port.PnpDeviceId, identifierValue, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(port.PortName, identifierValue, StringComparison.OrdinalIgnoreCase);
    }
}
