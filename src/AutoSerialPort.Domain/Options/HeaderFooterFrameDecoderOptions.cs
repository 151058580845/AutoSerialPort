namespace AutoSerialPort.Domain.Options;

/// <summary>
/// 包头包尾帧解码器配置选项
/// 用于配置基于包头和包尾标识的数据帧解码功能
/// </summary>
public class HeaderFooterFrameDecoderOptions
{
    /// <summary>
    /// 文本编码格式
    /// 默认为UTF-8编码
    /// </summary>
    public string Encoding { get; set; } = "utf-8";

    /// <summary>
    /// 包头标识
    /// 数据帧开始的标识字符或字符串
    /// </summary>
    public string Header { get; set; } = string.Empty;

    /// <summary>
    /// 包尾标识
    /// 数据帧结束的标识字符或字符串
    /// </summary>
    public string Footer { get; set; } = string.Empty;

    /// <summary>
    /// 是否在输出中包含包头和包尾
    /// 当设置为true时，解码后的数据帧会包含包头和包尾标识
    /// </summary>
    public bool IncludeHeaderFooter { get; set; } = false;

    /// <summary>
    /// 最大缓冲区长度（字节）
    /// 防止内存溢出，当缓冲区超过此长度时会清空缓冲区
    /// </summary>
    public int MaxBufferLength { get; set; } = 65536;
}
