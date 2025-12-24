using AutoSerialPort.Domain.Entities;

namespace AutoSerialPort.Domain.Models;

/// <summary>
/// 串口设备配置文件
/// 包含单个串口设备的完整配置信息
/// </summary>
public class SerialDeviceProfile
{
    /// <summary>
    /// 串口设备配置
    /// 包含串口连接参数和设备识别信息
    /// </summary>
    public SerialDeviceConfig Serial { get; set; } = new();

    /// <summary>
    /// 解析器配置
    /// 定义如何解析接收到的数据
    /// </summary>
    public ParserConfig Parser { get; set; } = new();

    /// <summary>
    /// 帧解码器配置
    /// 定义如何从数据流中提取完整的数据帧
    /// </summary>
    public FrameDecoderConfig FrameDecoder { get; set; } = new();

    /// <summary>
    /// 转发器配置列表
    /// 定义解析后的数据如何转发到各个目标
    /// </summary>
    public ForwarderConfig[] Forwarders { get; set; } = Array.Empty<ForwarderConfig>();
}
