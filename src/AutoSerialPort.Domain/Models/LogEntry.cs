namespace AutoSerialPort.Domain.Models;

/// <summary>
/// 日志条目模型
/// 表示应用程序中的一条日志记录
/// </summary>
public class LogEntry
{
    /// <summary>
    /// 日志时间戳
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 日志级别
    /// 如：Information、Warning、Error等
    /// </summary>
    public string Level { get; set; } = "Information";

    /// <summary>
    /// 日志消息内容
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 异常信息（可选）
    /// 当日志记录异常时包含异常详细信息
    /// </summary>
    public string? Exception { get; set; }
}
