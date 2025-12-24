namespace AutoSerialPort.Domain.Options;

/// <summary>
/// MQTT转发器配置选项
/// 用于配置MQTT消息队列数据转发功能
/// </summary>
public class MqttForwarderOptions
{
    /// <summary>
    /// MQTT代理服务器地址
    /// </summary>
    public string Broker { get; set; } = "localhost";

    /// <summary>
    /// MQTT代理服务器端口
    /// 默认端口1883（非加密），8883（TLS加密）
    /// </summary>
    public int Port { get; set; } = 1883;

    /// <summary>
    /// MQTT主题
    /// 消息发布的目标主题
    /// </summary>
    public string Topic { get; set; } = "demo/topic";

    /// <summary>
    /// 用户名（可选）
    /// 用于MQTT服务器身份验证
    /// </summary>
    public string? Username { get; set; } = "backend";

    /// <summary>
    /// 密码（可选）
    /// 用于MQTT服务器身份验证
    /// </summary>
    public string? Password { get; set; } = "5PibfhEhmoNXZcK2";

    /// <summary>
    /// 是否使用TLS加密连接
    /// </summary>
    public bool UseTls { get; set; }

    /// <summary>
    /// 服务质量等级
    /// 0：最多一次，1：至少一次，2：恰好一次
    /// </summary>
    public int QoS { get; set; }

    /// <summary>
    /// 是否保留消息
    /// 保留消息会在代理服务器上持久化
    /// </summary>
    public bool Retain { get; set; }
}
