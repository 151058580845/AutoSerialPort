namespace AutoSerialPort.Domain.Models;

/// <summary>
/// 串口设备状态信息
/// 包含设备的运行状态、统计信息和错误信息
/// </summary>
public class SerialDeviceStatus
{
    /// <summary>
    /// 设备ID
    /// </summary>
    public long DeviceId { get; set; }

    /// <summary>
    /// 设备显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 串口名称（如COM1、COM2等）
    /// </summary>
    public string? PortName { get; set; }

    /// <summary>
    /// 连接状态
    /// </summary>
    public SerialConnectionState ConnectionState { get; set; } = SerialConnectionState.Disconnected;

    /// <summary>
    /// 是否已启动。
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// 当前使用的解析器名称
    /// </summary>
    public string ParserName { get; set; } = string.Empty;

    /// <summary>
    /// 活跃的转发器列表
    /// </summary>
    public string[] ActiveForwarders { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 总消息数量
    /// </summary>
    public long TotalMessages { get; set; }

    /// <summary>
    /// 每秒消息数量（消息处理速率）
    /// </summary>
    public double MessagesPerSecond { get; set; }

    /// <summary>
    /// 最后一次错误信息
    /// </summary>
    public string? LastError { get; set; }
}
