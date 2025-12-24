using System;

namespace AutoSerialPort.Application.Models;

/// <summary>
/// 数据加载事件参数
/// </summary>
public class DataLoadingEventArgs : EventArgs
{
    /// <summary>
    /// 是否正在加载
    /// </summary>
    public bool IsLoading { get; init; }

    /// <summary>
    /// 操作名称
    /// </summary>
    public string? Operation { get; init; }

    /// <summary>
    /// 错误信息（如果有）
    /// </summary>
    public Exception? Error { get; init; }

    /// <summary>
    /// 设备数量
    /// </summary>
    public int DeviceCount { get; init; }

    /// <summary>
    /// 进度百分比 (0-100)
    /// </summary>
    public double Progress { get; init; } = 0;

    /// <summary>
    /// 是否显示进度条
    /// </summary>
    public bool ShowProgress { get; init; } = false;
}