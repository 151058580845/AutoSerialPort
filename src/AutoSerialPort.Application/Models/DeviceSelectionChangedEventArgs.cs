using System;
using AutoSerialPort.Application.Abstractions;

namespace AutoSerialPort.Application.Models;

/// <summary>
/// 设备选择变更事件参数
/// </summary>
public class DeviceSelectionChangedEventArgs : EventArgs
{
    /// <summary>
    /// 之前选中的设备
    /// </summary>
    public IDeviceProfileViewModel? PreviousDevice { get; init; }

    /// <summary>
    /// 当前选中的设备
    /// </summary>
    public IDeviceProfileViewModel? CurrentDevice { get; init; }

    /// <summary>
    /// 选择变更原因
    /// </summary>
    public SelectionChangeReason Reason { get; init; }

    /// <summary>
    /// 选择状态是否有效
    /// </summary>
    public bool IsValid { get; init; }
}

/// <summary>
/// 选择变更原因枚举
/// </summary>
public enum SelectionChangeReason
{
    /// <summary>
    /// 用户选择
    /// </summary>
    UserSelection,

    /// <summary>
    /// 数据刷新
    /// </summary>
    DataRefresh,

    /// <summary>
    /// 自动选择
    /// </summary>
    AutoSelection,

    /// <summary>
    /// 清除选择
    /// </summary>
    ClearSelection,

    /// <summary>
    /// 错误恢复
    /// </summary>
    ErrorRecovery
}