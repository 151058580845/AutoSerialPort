using SqlSugar;

namespace AutoSerialPort.Domain.Entities;

/// <summary>
/// 串口设备配置实体
/// 提供更高级的设备管理功能，支持多种设备识别方式和完整的串口参数配置
/// 相比 SerialPortConfig，增加了设备显示名称、多种识别方式和启用状态控制
/// 对应数据库表：serial_device_config
/// </summary>
[SugarTable("serial_device_config")]
public class SerialDeviceConfig
{
    /// <summary>
    /// 主键ID，自增长
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDataType = "INTEGER")]
    public long Id { get; set; }

    /// <summary>
    /// 所属配置文件ID
    /// 关联到 Profile 表，用于支持多套配置方案
    /// 默认值：1（默认配置文件）
    /// </summary>
    public long ProfileId { get; set; } = 1;

    /// <summary>
    /// 设备显示名称
    /// 用于在界面上显示的友好名称，便于用户识别不同设备
    /// 默认值：设备
    /// </summary>
    [SugarColumn(Length = 128)]
    public string DisplayName { get; set; } = "设备";

    /// <summary>
    /// 设备识别类型
    /// 可选值：
    /// - PortName：通过端口名称识别（如 COM3）
    /// - VidPid：通过 USB 设备的 VID/PID 识别
    /// - PnpDeviceId：通过即插即用设备ID识别
    /// 默认值：PortName
    /// </summary>
    [SugarColumn(Length = 32)]
    public string IdentifierType { get; set; } = "PortName";

    /// <summary>
    /// 设备识别值
    /// 根据 IdentifierType 的不同，存储不同格式的识别信息：
    /// - PortName 类型：COM3、COM4 等
    /// - VidPid 类型：VID_1234&PID_5678 格式
    /// - PnpDeviceId 类型：完整的 PnP 设备ID字符串
    /// 默认值：COM3
    /// </summary>
    [SugarColumn(Length = 256)]
    public string IdentifierValue { get; set; } = "COM3";

    /// <summary>
    /// 波特率（每秒传输的比特数）
    /// 常用值：9600、19200、38400、57600、115200
    /// 默认值：9600
    /// </summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>
    /// 校验位类型
    /// 可选值：None（无校验）、Odd（奇校验）、Even（偶校验）、Mark、Space
    /// 默认值：None
    /// </summary>
    [SugarColumn(Length = 16)]
    public string Parity { get; set; } = "None";

    /// <summary>
    /// 数据位长度
    /// 可选值：5、6、7、8
    /// 默认值：8
    /// </summary>
    public int DataBits { get; set; } = 8;

    /// <summary>
    /// 停止位
    /// 可选值：One（1位）、Two（2位）、OnePointFive（1.5位）
    /// 默认值：One
    /// </summary>
    [SugarColumn(Length = 16)]
    public string StopBits { get; set; } = "One";

    /// <summary>
    /// 是否启用该设备
    /// 用于控制设备是否参与数据采集
    /// 默认值：true（启用）
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
