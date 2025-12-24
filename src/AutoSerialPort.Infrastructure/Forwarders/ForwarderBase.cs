using System;
using System.Threading;
using System.Threading.Tasks;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Models;
using Serilog;

namespace AutoSerialPort.Infrastructure.Forwarders;

/// <summary>
/// 转发器基类，提供错误节流与默认生命周期实现。
/// </summary>
public abstract class ForwarderBase : IForwarder
{
    /// <summary>
    /// 错误节流器，避免刷屏。
    /// </summary>
    protected readonly ErrorThrottle ErrorThrottle = new(TimeSpan.FromSeconds(10));

    /// <summary>
    /// 转发器名称。
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool IsEnabled { get; protected set; }

    /// <summary>
    /// 启动转发器。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    public virtual Task StartAsync(CancellationToken ct) => Task.CompletedTask;

    /// <summary>
    /// 停止转发器。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    public virtual Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    /// <summary>
    /// 转发一条消息。
    /// </summary>
    /// <param name="message">解析消息。</param>
    /// <param name="ct">取消令牌。</param>
    public abstract Task ForwardAsync(ParsedMessage message, CancellationToken ct);

    /// <summary>
    /// 记录错误日志，带节流控制。
    /// </summary>
    /// <param name="ex">异常。</param>
    /// <param name="message">日志内容。</param>
    protected void LogError(Exception ex, string message)
    {
        if (ErrorThrottle.ShouldLog())
        {
            Log.Error(ex, message);
        }
    }

    /// <summary>
    /// 记录警告日志，带节流控制。
    /// </summary>
    /// <param name="message">日志内容。</param>
    protected void LogWarning(string message)
    {
        if (ErrorThrottle.ShouldLog())
        {
            Log.Warning(message);
        }
    }
}
