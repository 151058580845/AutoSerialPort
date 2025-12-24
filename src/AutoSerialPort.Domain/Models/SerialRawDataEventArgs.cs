using System;

namespace AutoSerialPort.Domain.Models;

/// <summary>
/// 原始串口数据事件参数。
/// </summary>
public sealed class SerialRawDataEventArgs : EventArgs
{
    /// <summary>
    /// 创建设备原始数据事件参数。
    /// </summary>
    /// <param name="deviceId">设备 ID。</param>
    /// <param name="data">原始字节数据。</param>
    /// <param name="timestamp">接收时间。</param>
    public SerialRawDataEventArgs(long deviceId, byte[] data, DateTimeOffset timestamp)
    {
        DeviceId = deviceId;
        Data = data;
        Timestamp = timestamp;
    }

    /// <summary>
    /// 设备 ID。
    /// </summary>
    public long DeviceId { get; }

    /// <summary>
    /// 原始数据。
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// 接收时间戳。
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}
