namespace AutoSerialPort.Domain.Options;

/// <summary>
/// 键盘输入转发器配置选项
/// 用于配置模拟键盘输入的数据转发功能
/// </summary>
public class TypingForwarderOptions
{
    /// <summary>
    /// 输入延迟时间（毫秒）
    /// 在模拟键盘输入前等待的时间，用于确保目标应用程序准备就绪
    /// </summary>
    public int DelayMs { get; set; } = 0;
}
