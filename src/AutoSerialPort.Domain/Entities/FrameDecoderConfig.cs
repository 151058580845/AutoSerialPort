using SqlSugar;

namespace AutoSerialPort.Domain.Entities;

/// <summary>
/// 帧解码器配置实体
/// 用于配置串口数据的帧解码方式，解决串口通信中的粘包和半包问题
/// 帧解码器负责将连续的字节流拆分成独立的数据帧
/// 对应数据库表：frame_decoder_config
/// </summary>
[SugarTable("frame_decoder_config")]
public class FrameDecoderConfig
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
    /// 关联的设备ID
    /// 关联到 SerialDeviceConfig 表，指定该解码器配置属于哪个设备
    /// 值为 0 时表示全局配置
    /// </summary>
    public long DeviceId { get; set; } = 0;

    /// <summary>
    /// 解码器类型
    /// 可选值：
    /// - DelimiterFrameDecoder：分隔符解码器，按指定分隔符（如换行符）拆分数据帧
    /// - HeaderFooterFrameDecoder：包头包尾解码器，按固定的包头和包尾标识拆分
    /// - FixedLengthFrameDecoder：固定长度解码器，按固定字节长度拆分
    /// - NoFrameDecoder：不拆包，直接使用原始数据
    /// 默认值：DelimiterFrameDecoder
    /// </summary>
    [SugarColumn(Length = 64)]
    public string DecoderType { get; set; } = "DelimiterFrameDecoder";

    /// <summary>
    /// 解码器参数JSON字符串
    /// 存储解码器的详细配置参数，不同类型的解码器有不同的参数：
    /// - DelimiterFrameDecoder：encoding（编码）、delimiter（分隔符）、includeDelimiter（是否包含分隔符）、maxBufferLength（最大缓冲区长度）
    /// - HeaderFooterFrameDecoder：header（包头）、footer（包尾）、encoding（编码）
    /// - FixedLengthFrameDecoder：length（固定长度）
    /// - NoFrameDecoder：无需参数
    /// 默认值：使用换行符作为分隔符的 UTF-8 编码配置
    /// </summary>
    [SugarColumn(ColumnDataType = "TEXT")]
    public string ParametersJson { get; set; } = "{\"encoding\":\"utf-8\",\"delimiter\":\"\\n\",\"includeDelimiter\":true,\"maxBufferLength\":65536}";
}
