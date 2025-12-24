using AutoSerialPort.Domain.Abstractions;

namespace AutoSerialPort.Infrastructure.Framing;

/// <summary>
/// 不拆包解码器提供者。
/// </summary>
public class NoFrameDecoderProvider : IFrameDecoderProvider
{
    /// <summary>
    /// 解码器类型标识。
    /// </summary>
    public string Type => "NoFrameDecoder";

    /// <summary>
    /// 界面展示名称。
    /// </summary>
    public string DisplayName => "不拆包";

    /// <summary>
    /// 默认参数 JSON。
    /// </summary>
    public string DefaultParametersJson => "{}";

    /// <summary>
    /// 创建解码器实例。
    /// </summary>
    public IFrameDecoder Create(string? parametersJson)
        => new NoFrameDecoder();
}
