namespace AutoSerialPort.Domain.Models;

/// <summary>
/// 串口连接状态枚举
/// 定义串口设备的各种连接状态
/// </summary>
public enum SerialConnectionState
{
    /// <summary>
    /// 已断开连接
    /// </summary>
    Disconnected = 0,

    /// <summary>
    /// 正在连接中
    /// </summary>
    Connecting = 1,

    /// <summary>
    /// 已连接
    /// </summary>
    Connected = 2,

    /// <summary>
    /// 正在重新连接
    /// </summary>
    Reconnecting = 3
}
