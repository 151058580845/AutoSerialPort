using System.Collections.Generic;
using AutoSerialPort.Application.Models;

namespace AutoSerialPort.Application.Abstractions;

/// <summary>
/// 转发器设置视图模型接口
/// 提供转发器设置的抽象表示，避免Application层直接依赖UI层
/// </summary>
public interface IForwarderSettingsViewModel
{
    // TCP转发相关属性
    /// <summary>
    /// TCP转发是否启用
    /// </summary>
    bool TcpEnabled { get; set; }

    /// <summary>
    /// TCP模式选项
    /// </summary>
    IReadOnlyList<OptionItem> TcpModeOptions { get; }

    /// <summary>
    /// 选中的TCP模式选项
    /// </summary>
    OptionItem? SelectedTcpModeOption { get; set; }

    /// <summary>
    /// TCP主机
    /// </summary>
    string TcpHost { get; set; }

    /// <summary>
    /// TCP端口
    /// </summary>
    int TcpPort { get; set; }

    // MQTT转发相关属性
    /// <summary>
    /// MQTT转发是否启用
    /// </summary>
    bool MqttEnabled { get; set; }

    /// <summary>
    /// MQTT服务器
    /// </summary>
    string MqttBroker { get; set; }

    /// <summary>
    /// MQTT端口
    /// </summary>
    int MqttPort { get; set; }

    /// <summary>
    /// MQTT主题
    /// </summary>
    string MqttTopic { get; set; }

    /// <summary>
    /// MQTT用户名
    /// </summary>
    string MqttUsername { get; set; }

    /// <summary>
    /// MQTT密码
    /// </summary>
    string MqttPassword { get; set; }

    /// <summary>
    /// MQTT是否使用TLS
    /// </summary>
    bool MqttUseTls { get; set; }

    /// <summary>
    /// QoS选项
    /// </summary>
    int[] QosOptions { get; }

    /// <summary>
    /// MQTT QoS
    /// </summary>
    int? MqttQos { get; set; }

    /// <summary>
    /// MQTT是否保留
    /// </summary>
    bool MqttRetain { get; set; }

    // 剪贴板转发相关属性
    /// <summary>
    /// 剪贴板转发是否启用
    /// </summary>
    bool ClipboardEnabled { get; set; }

    /// <summary>
    /// 剪贴板是否追加换行
    /// </summary>
    bool ClipboardAppendNewLine { get; set; }

    // 模拟输入相关属性
    /// <summary>
    /// 模拟输入是否启用
    /// </summary>
    bool TypingEnabled { get; set; }

    /// <summary>
    /// 模拟输入延迟(毫秒)
    /// </summary>
    int TypingDelayMs { get; set; }
}
