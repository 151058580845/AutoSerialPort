using System.Text.Json;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Options;

namespace AutoSerialPort.Infrastructure.Framing;

/// <summary>
/// 分隔符拆包解码器提供者。
/// </summary>
public class DelimiterFrameDecoderProvider : IFrameDecoderProvider
{
    /// <summary>
    /// JSON 序列化选项，使用 camelCase 命名策略以匹配数据库中的格式。
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 解码器类型标识。
    /// </summary>
    public string Type => "DelimiterFrameDecoder";

    /// <summary>
    /// 界面展示名称。
    /// </summary>
    public string DisplayName => "分隔符拆包";

    /// <summary>
    /// 默认参数 JSON。
    /// </summary>
    public string DefaultParametersJson => JsonSerializer.Serialize(new DelimiterFrameDecoderOptions(), JsonOptions);

    /// <summary>
    /// 创建解码器实例。
    /// </summary>
    /// <param name="parametersJson">配置参数 JSON。</param>
    public IFrameDecoder Create(string? parametersJson)
    {
        var options = Deserialize(parametersJson, new DelimiterFrameDecoderOptions());
        return new DelimiterFrameDecoder(options);
    }

    /// <summary>
    /// 反序列化 JSON 参数，失败则回退到默认值。
    /// </summary>
    private static T Deserialize<T>(string? json, T fallback) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return fallback;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }
}
