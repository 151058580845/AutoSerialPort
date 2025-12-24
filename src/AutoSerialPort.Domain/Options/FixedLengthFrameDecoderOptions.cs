namespace AutoSerialPort.Domain.Options;

/// <summary>
/// 固定长度帧解码器配置选项
/// 用于配置基于固定长度的数据帧解码功能
/// </summary>
public class FixedLengthFrameDecoderOptions
{
    /// <summary>
    /// 数据帧长度（字节）
    /// 每个数据帧的固定字节数
    /// </summary>
    public int FrameLength { get; set; } = 16;

    /// <summary>
    /// 最大缓冲区长度（字节）
    /// 防止内存溢出，当缓冲区超过此长度时会清空缓冲区
    /// </summary>
    public int MaxBufferLength { get; set; } = 65536;
}
