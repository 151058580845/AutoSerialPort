namespace AutoSerialPort.Domain.Models;

/// <summary>
/// 解析后的消息模型
/// 表示从串口接收并经过解析器处理后的数据
/// </summary>
public class ParsedMessage
{
    /// <summary>
    /// 解析后的文本内容
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 原始字节数据（可选）
    /// 保留原始数据用于调试或特殊处理
    /// </summary>
    public byte[]? Raw { get; set; }

    /// <summary>
    /// 消息时间戳
    /// 记录消息接收和解析的时间
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
}
