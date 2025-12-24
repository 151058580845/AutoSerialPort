namespace AutoSerialPort.Domain.Options;

/// <summary>
/// 剪贴板转发器配置选项
/// 用于配置将数据转发到系统剪贴板的功能
/// </summary>
public class ClipboardForwarderOptions
{
    /// <summary>
    /// 是否在文本末尾添加换行符
    /// 当设置为true时，会在复制到剪贴板的文本末尾添加换行符
    /// </summary>
    public bool AppendNewLine { get; set; }
}
