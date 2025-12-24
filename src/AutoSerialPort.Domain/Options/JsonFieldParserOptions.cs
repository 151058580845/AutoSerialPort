namespace AutoSerialPort.Domain.Options;

/// <summary>
/// JSON字段解析器配置选项
/// 用于配置从JSON数据中提取特定字段的解析功能
/// </summary>
public class JsonFieldParserOptions
{
    /// <summary>
    /// 文本编码格式
    /// 默认为UTF-8编码
    /// </summary>
    public string Encoding { get; set; } = "utf-8";

    /// <summary>
    /// 行分隔符
    /// 用于分割JSON行的字符，默认为换行符
    /// </summary>
    public string Separator { get; set; } = "\n";

    /// <summary>
    /// JSON字段路径
    /// 使用点号分隔的字段路径，如"data.value"表示提取data对象中的value字段
    /// </summary>
    public string FieldPath { get; set; } = "data";

    /// <summary>
    /// 是否允许宽松的JSON解析
    /// 当设置为true时，允许解析不严格符合JSON标准的数据
    /// </summary>
    public bool AllowLooseJson { get; set; } = true;
}
