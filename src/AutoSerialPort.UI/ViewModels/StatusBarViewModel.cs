using System.Linq;
using AutoSerialPort.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoSerialPort.UI.ViewModels;

/// <summary>
/// 状态栏视图模型，展示全局连接与吞吐信息。
/// </summary>
public partial class StatusBarViewModel : ObservableObject
{
    [ObservableProperty]
    private string _connectionStateText = "未连接";

    [ObservableProperty]
    private string _parserName = string.Empty;

    [ObservableProperty]
    private string _forwardersText = string.Empty;

    [ObservableProperty]
    private string _throughputText = "0.0 条/秒";

    [ObservableProperty]
    private string _lastErrorText = string.Empty;

    [ObservableProperty]
    private bool _hasLastError;

    /// <summary>
    /// 根据状态快照刷新显示。
    /// </summary>
    /// <param name="snapshot">状态快照。</param>
    public void Update(SerialStatusSnapshot snapshot)
    {
        var deviceCount = snapshot.DeviceStatuses.Length;
        if (deviceCount == 0)
        {
            ConnectionStateText = "未连接";
            ParserName = "无";
        }
        else
        {
            var connected = snapshot.DeviceStatuses.Count(x => x.ConnectionState == SerialConnectionState.Connected);
            ConnectionStateText = $"{connected}/{deviceCount} 已连接";
            ParserName = deviceCount == 1 ? snapshot.DeviceStatuses[0].ParserName : "多路";
        }

        ForwardersText = snapshot.ActiveForwarders.Length == 0
            ? "无"
            : string.Join(", ", snapshot.ActiveForwarders);
        ThroughputText = $"{snapshot.MessagesPerSecond:F1} 条/秒 (总计 {snapshot.TotalMessages})";
        LastErrorText = snapshot.LastError ?? string.Empty;
        HasLastError = !string.IsNullOrWhiteSpace(LastErrorText);
    }
}
