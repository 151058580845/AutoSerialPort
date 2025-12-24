using System;

namespace AutoSerialPort.Application.Models;

/// <summary>
/// 设备选择异常
/// </summary>
public class DeviceSelectionException : Exception
{
    /// <summary>
    /// 设备ID
    /// </summary>
    public long? DeviceId { get; init; }

    /// <summary>
    /// 显示名称
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// 错误类型
    /// </summary>
    public SelectionErrorType ErrorType { get; init; }

    /// <summary>
    /// 初始化设备选择异常
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="errorType">错误类型</param>
    public DeviceSelectionException(string message, SelectionErrorType errorType) : base(message)
    {
        ErrorType = errorType;
    }

    /// <summary>
    /// 初始化设备选择异常
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="errorType">错误类型</param>
    /// <param name="innerException">内部异常</param>
    public DeviceSelectionException(string message, SelectionErrorType errorType, Exception innerException) 
        : base(message, innerException)
    {
        ErrorType = errorType;
    }
}

/// <summary>
/// 选择错误类型枚举
/// </summary>
public enum SelectionErrorType
{
    /// <summary>
    /// 设备未找到
    /// </summary>
    DeviceNotFound,

    /// <summary>
    /// 无效状态
    /// </summary>
    InvalidState,

    /// <summary>
    /// 并发冲突
    /// </summary>
    ConcurrencyConflict,

    /// <summary>
    /// 数据加载失败
    /// </summary>
    DataLoadFailure,

    /// <summary>
    /// 验证失败
    /// </summary>
    ValidationFailure
}