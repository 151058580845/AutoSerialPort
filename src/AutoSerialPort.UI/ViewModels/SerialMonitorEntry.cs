using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoSerialPort.UI.ViewModels;

/// <summary>
/// 串口接收区显示条目。
/// </summary>
public partial class SerialMonitorEntry : ObservableObject
{
    /// <summary>
    /// 创建接收条目。
    /// </summary>
    /// <param name="timestamp">接收时间。</param>
    /// <param name="data">原始数据。</param>
    /// <param name="displayText">显示文本。</param>
    public SerialMonitorEntry(DateTimeOffset timestamp, byte[] data, string displayText)
    {
        Timestamp = timestamp;
        Data = data;
        _displayText = displayText;
        _timestampText = timestamp.ToLocalTime().ToString("HH:mm:ss.fff");
    }

    /// <summary>
    /// 接收时间。
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// 原始数据。
    /// </summary>
    public byte[] Data { get; }

    [ObservableProperty]
    private string _displayText;

    [ObservableProperty]
    private string _timestampText;
}
