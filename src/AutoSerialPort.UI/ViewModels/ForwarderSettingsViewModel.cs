using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AutoSerialPort.Application.Abstractions;
using AutoSerialPort.Domain.Entities;
using AutoSerialPort.Domain.Options;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoSerialPort.UI.ViewModels;

/// <summary>
/// 转发设置视图模型，管理各类转发器参数。
/// </summary>
public partial class ForwarderSettingsViewModel : ObservableObject, IForwarderSettingsViewModel
{
    private long _profileId = 1;
    private long _deviceId = 0;

    private long _tcpId;
    private long _mqttId;
    private long _clipboardId;
    private long _typingId;

    [ObservableProperty]
    private bool _tcpEnabled;

    [ObservableProperty]
    private string _tcpMode = "Server";

    [ObservableProperty]
    private string _tcpHost = "0.0.0.0";

    [ObservableProperty]
    private int _tcpPort = 9000;

    [ObservableProperty]
    private bool _mqttEnabled;

    [ObservableProperty]
    private string _mqttBroker = "localhost";

    [ObservableProperty]
    private int _mqttPort = 1883;

    [ObservableProperty]
    private string _mqttTopic = "demo/topic";

    [ObservableProperty]
    private string? _mqttUsername;

    [ObservableProperty]
    private string? _mqttPassword;

    [ObservableProperty]
    private bool _mqttUseTls;

    [ObservableProperty]
    private int? _mqttQos = 0;

    [ObservableProperty]
    private bool _mqttRetain;

    [ObservableProperty]
    private bool _clipboardEnabled;

    [ObservableProperty]
    private bool _clipboardAppendNewLine;

    [ObservableProperty]
    private bool _typingEnabled;

    [ObservableProperty]
    private int _typingDelayMs;

    public IReadOnlyList<AutoSerialPort.Application.Models.OptionItem> TcpModeOptions { get; } = new[]
    {
        new AutoSerialPort.Application.Models.OptionItem("Server", "服务端"),
        new AutoSerialPort.Application.Models.OptionItem("Client", "客户端")
    };

    public int[] QosOptions { get; } = new[] { 0, 1, 2 };

    [ObservableProperty]
    private AutoSerialPort.Application.Models.OptionItem? _selectedTcpModeOption;

    /// <summary>
    /// 加载转发器配置。
    /// </summary>
    /// <param name="configs">转发器配置集合。</param>
    /// <param name="deviceId">设备 Id。</param>
    public void Load(ForwarderConfig[] configs, long deviceId)
    {
        _deviceId = deviceId;
        foreach (var config in configs)
        {
            _profileId = config.ProfileId;

            if (string.Equals(config.ForwarderType, "TcpForwarder", StringComparison.OrdinalIgnoreCase))
            {
                _tcpId = config.Id;
                TcpEnabled = config.IsEnabled;
                var options = Deserialize(config.ParametersJson, new TcpForwarderOptions());
                TcpMode = options.Mode;
                TcpHost = options.Host;
                TcpPort = options.Port;
                SelectedTcpModeOption = TcpModeOptions.FirstOrDefault(x =>
                    string.Equals(x.Value, TcpMode, StringComparison.OrdinalIgnoreCase)) ?? TcpModeOptions.FirstOrDefault();
            }

            if (string.Equals(config.ForwarderType, "MqttForwarder", StringComparison.OrdinalIgnoreCase))
            {
                _mqttId = config.Id;
                MqttEnabled = config.IsEnabled;
                var options = Deserialize(config.ParametersJson, new MqttForwarderOptions());
                MqttBroker = options.Broker;
                MqttPort = options.Port;
                MqttTopic = options.Topic;
                MqttUsername = options.Username;
                MqttPassword = options.Password;
                MqttUseTls = options.UseTls;
                MqttQos = options.QoS;
                MqttRetain = options.Retain;
            }

            if (string.Equals(config.ForwarderType, "ClipboardForwarder", StringComparison.OrdinalIgnoreCase))
            {
                _clipboardId = config.Id;
                ClipboardEnabled = config.IsEnabled;
                var options = Deserialize(config.ParametersJson, new ClipboardForwarderOptions());
                ClipboardAppendNewLine = options.AppendNewLine;
            }

            if (string.Equals(config.ForwarderType, "TypingForwarder", StringComparison.OrdinalIgnoreCase))
            {
                _typingId = config.Id;
                TypingEnabled = config.IsEnabled;
                var options = Deserialize(config.ParametersJson, new TypingForwarderOptions());
                TypingDelayMs = options.DelayMs;
            }
        }
    }

    /// <summary>
    /// 构建转发器配置集合。
    /// </summary>
    public ForwarderConfig[] BuildConfigs()
    {
        var tcpMode = SelectedTcpModeOption?.Value ?? TcpMode;
        TcpMode = tcpMode;
        var list = new List<ForwarderConfig>
        {
            new()
            {
                Id = _tcpId,
                ProfileId = _profileId,
                DeviceId = _deviceId,
                ForwarderType = "TcpForwarder",
                IsEnabled = TcpEnabled,
                ParametersJson = JsonSerializer.Serialize(new TcpForwarderOptions
                {
                    Mode = tcpMode,
                    Host = TcpHost,
                    Port = TcpPort
                }, JsonOptions)
            },
            new()
            {
                Id = _mqttId,
                ProfileId = _profileId,
                DeviceId = _deviceId,
                ForwarderType = "MqttForwarder",
                IsEnabled = MqttEnabled,
                ParametersJson = JsonSerializer.Serialize(new MqttForwarderOptions
                {
                    Broker = MqttBroker,
                    Port = MqttPort,
                    Topic = MqttTopic,
                    Username = MqttUsername,
                    Password = MqttPassword,
                    UseTls = MqttUseTls,
                    QoS = MqttQos ?? 0,
                    Retain = MqttRetain
                }, JsonOptions)
            },
            new()
            {
                Id = _clipboardId,
                ProfileId = _profileId,
                DeviceId = _deviceId,
                ForwarderType = "ClipboardForwarder",
                IsEnabled = ClipboardEnabled,
                ParametersJson = JsonSerializer.Serialize(new ClipboardForwarderOptions
                {
                    AppendNewLine = ClipboardAppendNewLine
                }, JsonOptions)
            },
            new()
            {
                Id = _typingId,
                ProfileId = _profileId,
                DeviceId = _deviceId,
                ForwarderType = "TypingForwarder",
                IsEnabled = TypingEnabled,
                ParametersJson = JsonSerializer.Serialize(new TypingForwarderOptions
                {
                    DelayMs = TypingDelayMs
                }, JsonOptions)
            }
        };

        return list.ToArray();
    }

    /// <summary>
    /// JSON 序列化选项，使用 camelCase 命名策略以匹配数据库中的格式。
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 反序列化 JSON 参数，失败则回退到默认值。
    /// </summary>
    private static T Deserialize<T>(string json, T fallback) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }

    /// <summary>
    /// 处理 TCP 模式选项变更。
    /// </summary>
    partial void OnSelectedTcpModeOptionChanged(AutoSerialPort.Application.Models.OptionItem? value)
    {
        if (value != null)
        {
            TcpMode = value.Value;
        }
    }
}
