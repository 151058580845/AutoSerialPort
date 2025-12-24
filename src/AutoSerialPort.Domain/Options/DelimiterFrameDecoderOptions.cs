namespace AutoSerialPort.Domain.Options;

/// <summary>
/// 分隔符帧解码器配置选项
/// 用于配置基于分隔符的数据帧解码功能
/// </summary>
public class DelimiterFrameDecoderOptions
{
    /// <summary>
    /// 文本编码格式
    /// 默认为UTF-8编码
    /// </summary>
    public string Encoding { get; set; } = "utf-8";

    /// <summary>
    /// 帧分隔符
    /// 用于分割数据帧的字符或字符串，默认为换行符
    /// </summary>
    public string Delimiter { get; set; } = "\n";

    /// <summary>
    /// 是否在输出中包含分隔符
    /// 当设置为true时，解码后的数据帧会包含分隔符
    /// </summary>
    public bool IncludeDelimiter { get; set; } = true;

    /// <summary>
    /// 最大缓冲区长度（字节）
    /// 防止内存溢出，当缓冲区超过此长度时会清空缓冲区
    /// </summary>
    public int MaxBufferLength { get; set; } = 65536;
}
