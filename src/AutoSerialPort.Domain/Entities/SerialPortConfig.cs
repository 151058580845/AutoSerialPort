using SqlSugar;

namespace AutoSerialPort.Domain.Entities;

/// <summary>
/// 串口基础配置实体
/// 存储串口通信的基本参数配置，包括端口名、波特率、校验位、数据位、停止位等
/// 对应数据库表：serial_port_config
/// </summary>
[SugarTable("serial_port_config")]
public class SerialPortConfig
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
    /// 串口端口名称
    /// 如：COM1、COM2、COM3 等（Windows）或 /dev/ttyUSB0（Linux）
    /// 默认值：COM3
    /// </summary>
    [SugarColumn(Length = 64)]
    public string PortName { get; set; } = "COM3";

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
}
