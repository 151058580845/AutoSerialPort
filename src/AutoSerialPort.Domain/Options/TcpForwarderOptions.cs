namespace AutoSerialPort.Domain.Options;

/// <summary>
/// TCP转发器配置选项
/// 用于配置TCP网络数据转发功能
/// </summary>
public class TcpForwarderOptions
{
    /// <summary>
    /// TCP工作模式
    /// 可选值：Server（服务器模式）、Client（客户端模式）
    /// </summary>
    public string Mode { get; set; } = "Server";

    /// <summary>
    /// 主机地址
    /// 服务器模式下为监听地址，客户端模式下为连接地址
    /// </summary>
    public string Host { get; set; } = "0.0.0.0";

    /// <summary>
    /// 端口号
    /// 服务器模式下为监听端口，客户端模式下为连接端口
    /// </summary>
    public int Port { get; set; } = 9000;
}
