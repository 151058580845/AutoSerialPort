using System.Text.Json;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Options;

namespace AutoSerialPort.Infrastructure.Parsing;

/// <summary>
/// 行解析器提供者。
/// </summary>
public class LineParserProvider : IParserProvider
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
    /// 解析器类型标识。
    /// </summary>
    public string Type => "LineParser";

    /// <summary>
    /// 界面展示名称。
    /// </summary>
    public string DisplayName => "行解析";

    /// <summary>
    /// 默认参数 JSON。
    /// </summary>
    public string DefaultParametersJson => JsonSerializer.Serialize(new LineParserOptions
    {
        Encoding = "utf-8",
        Separator = "\n"
    }, JsonOptions);

    /// <summary>
    /// 创建解析器实例。
    /// </summary>
    /// <param name="parametersJson">配置参数 JSON。</param>
    public IParser Create(string parametersJson)
    {
        var options = Deserialize(parametersJson, new LineParserOptions());
        return new LineParser(options);
    }

    /// <summary>
    /// 反序列化 JSON 参数，失败则回退到默认值。
    /// </summary>
    private static T Deserialize<T>(string json, T fallback) where T : class
    {
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
