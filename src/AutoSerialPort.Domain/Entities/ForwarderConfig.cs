using SqlSugar;

namespace AutoSerialPort.Domain.Entities;

/// <summary>
/// 转发器配置实体
/// 用于配置解析后数据的输出方式，支持多种数据转发通道
/// 转发器是数据处理流水线的最后一环，负责将处理后的数据发送到目标位置
/// 对应数据库表：forwarder_config
/// </summary>
[SugarTable("forwarder_config")]
public class ForwarderConfig
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
    /// 关联到 SerialDeviceConfig 表，指定该转发器配置属于哪个设备
    /// 值为 0 时表示全局配置
    /// </summary>
    public long DeviceId { get; set; } = 0;

    /// <summary>
    /// 转发器类型
    /// 可选值：
    /// - TcpForwarder：TCP 转发器，支持服务器模式和客户端模式
    /// - MqttForwarder：MQTT 转发器，将数据发布到 MQTT 主题
    /// - ClipboardForwarder：剪贴板转发器，将数据复制到系统剪贴板
    /// - TypingForwarder：模拟键盘输入转发器，将数据作为键盘输入发送
    /// 默认值：TcpForwarder
    /// </summary>
    [SugarColumn(Length = 64)]
    public string ForwarderType { get; set; } = "TcpForwarder";

    /// <summary>
    /// 是否启用该转发器
    /// 用于控制转发器是否工作
    /// 默认值：false（不启用）
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// 转发器参数JSON字符串
    /// 存储转发器的详细配置参数，不同类型的转发器有不同的参数：
    /// - TcpForwarder：mode（Server/Client）、host（主机地址）、port（端口号）
    /// - MqttForwarder：broker（服务器地址）、port（端口）、topic（主题）、clientId（客户端ID）、username（用户名）、password（密码）
    /// - ClipboardForwarder：appendNewLine（是否追加换行符）
    /// - TypingForwarder：delay（按键延迟）、appendEnter（是否追加回车键）
    /// 默认值：TCP 服务器模式，监听 0.0.0.0:9000
    /// </summary>
    [SugarColumn(ColumnDataType = "TEXT")]
    public string ParametersJson { get; set; } = "{\"mode\":\"Server\",\"host\":\"0.0.0.0\",\"port\":9000}";
}
