using SqlSugar;

namespace AutoSerialPort.Domain.Entities;

/// <summary>
/// 解析器配置实体
/// 用于配置数据帧的解析方式，将原始数据帧转换为结构化的业务数据
/// 解析器在帧解码器之后工作，负责提取和转换数据
/// 对应数据库表：parser_config
/// </summary>
[SugarTable("parser_config")]
public class ParserConfig
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
    /// 关联到 SerialDeviceConfig 表，指定该解析器配置属于哪个设备
    /// 值为 0 时表示全局配置
    /// </summary>
    public long DeviceId { get; set; } = 0;

    /// <summary>
    /// 解析器类型
    /// 可选值：
    /// - LineParser：行解析器，按行处理文本数据
    /// - JsonFieldParser：JSON字段解析器，从JSON数据中提取指定字段
    /// - ScaleParser：电子秤解析器，专门解析电子秤协议数据
    /// - BarcodeParser：条码解析器，专门解析条码扫描器数据
    /// 默认值：LineParser
    /// </summary>
    [SugarColumn(Length = 64)]
    public string ParserType { get; set; } = "LineParser";

    /// <summary>
    /// 解析器参数JSON字符串
    /// 存储解析器的详细配置参数，不同类型的解析器有不同的参数：
    /// - LineParser：encoding（编码）、separator（分隔符）
    /// - JsonFieldParser：fieldPath（JSON字段路径）、encoding（编码）
    /// - ScaleParser：protocol（协议类型）、unit（单位）
    /// - BarcodeParser：prefix（前缀）、suffix（后缀）、encoding（编码）
    /// 默认值：UTF-8 编码，换行符分隔
    /// </summary>
    [SugarColumn(ColumnDataType = "TEXT")]
    public string ParametersJson { get; set; } = "{\"encoding\":\"utf-8\",\"separator\":\"\\n\"}";
}
