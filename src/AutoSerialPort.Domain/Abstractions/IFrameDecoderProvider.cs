namespace AutoSerialPort.Domain.Abstractions;

/// <summary>
/// 拆包解码器提供者，用于创建指定类型的解码器。
/// </summary>
public interface IFrameDecoderProvider
{
    /// <summary>
    /// 解码器类型标识。
    /// </summary>
    string Type { get; }

    /// <summary>
    /// 界面展示名称。
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// 默认参数 JSON。
    /// </summary>
    string DefaultParametersJson { get; }

    /// <summary>
    /// 创建解码器实例。
    /// </summary>
    /// <param name="parametersJson">配置参数 JSON。</param>
    IFrameDecoder Create(string? parametersJson);
}
