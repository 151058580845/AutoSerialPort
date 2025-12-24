namespace AutoSerialPort.Domain.Options;

/// <summary>
/// 电子秤解析器配置选项
/// 用于配置电子秤数据的解析功能
/// </summary>
public class ScaleParserOptions
{
    /// <summary>
    /// 文本编码格式
    /// 默认为UTF-8编码
    /// </summary>
    public string Encoding { get; set; } = "utf-8";

    /// <summary>
    /// 是否去除空白字符
    /// 当设置为true时，会自动去除数据前后的空格、制表符等空白字符
    /// </summary>
    public bool TrimWhitespace { get; set; } = true;
}
