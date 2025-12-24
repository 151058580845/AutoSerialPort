using System.Threading;
using System.Threading.Tasks;
using AutoSerialPort.Domain.Models;

namespace AutoSerialPort.Domain.Abstractions;

/// <summary>
/// 转发器接口，负责将解析后的数据输出到目标通道。
/// </summary>
public interface IForwarder
{
    /// <summary>
    /// 转发器名称，用于界面显示。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// 启动转发器。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    Task StartAsync(CancellationToken ct);

    /// <summary>
    /// 停止转发器。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    Task StopAsync(CancellationToken ct);

    /// <summary>
    /// 转发一条解析消息。
    /// </summary>
    /// <param name="message">解析后的消息。</param>
    /// <param name="ct">取消令牌。</param>
    Task ForwardAsync(ParsedMessage message, CancellationToken ct);
}
