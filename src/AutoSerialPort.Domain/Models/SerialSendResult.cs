namespace AutoSerialPort.Domain.Models;

/// <summary>
/// 串口发送结果。
/// </summary>
public sealed class SerialSendResult
{
    private SerialSendResult(bool success, string? error)
    {
        Success = success;
        Error = error;
    }

    /// <summary>
    /// 是否发送成功。
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// 失败原因。
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// 创建成功结果。
    /// </summary>
    public static SerialSendResult Ok() => new(true, null);

    /// <summary>
    /// 创建失败结果。
    /// </summary>
    /// <param name="error">错误信息。</param>
    public static SerialSendResult Fail(string error) => new(false, error);
}
