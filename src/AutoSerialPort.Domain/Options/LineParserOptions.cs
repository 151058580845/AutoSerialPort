namespace AutoSerialPort.Domain.Options;

/// <summary>
/// 行解析器配置选项
/// 用于配置基于行的文本数据解析
/// </summary>
public class LineParserOptions
{
    /// <summary>
    /// 文本编码格式
    /// 默认为UTF-8编码
    /// </summary>
    public string Encoding { get; set; } = "utf-8";

    /// <summary>
    /// 行分隔符
    /// 用于分割文本行的字符，默认为换行符
    /// </summary>
    public string Separator { get; set; } = "\n";
}
