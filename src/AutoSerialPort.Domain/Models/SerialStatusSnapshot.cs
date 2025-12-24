namespace AutoSerialPort.Domain.Models;

/// <summary>
/// 串口状态快照
/// 包含整个串口服务的状态信息和所有设备的状态
/// </summary>
public class SerialStatusSnapshot
{
    /// <summary>
    /// 整体连接状态
    /// </summary>
    public SerialConnectionState ConnectionState { get; set; } = SerialConnectionState.Disconnected;

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

    /// <summary>
    /// 所有设备的状态信息
    /// </summary>
    public SerialDeviceStatus[] DeviceStatuses { get; set; } = Array.Empty<SerialDeviceStatus>();
}
