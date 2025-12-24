using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Models;
using Serilog.Core;
using Serilog.Events;

namespace AutoSerialPort.Infrastructure.Logging;

/// <summary>
/// Serilog 内存接收器，将日志写入 UI 缓冲区。
/// </summary>
public class InMemoryLogSink : ILogEventSink
{
    private readonly ILogBuffer _buffer;

    /// <summary>
    /// 创建内存日志接收器。
    /// </summary>
    /// <param name="buffer">日志缓冲区。</param>
    public InMemoryLogSink(ILogBuffer buffer)
    {
        _buffer = buffer;
    }

    /// <summary>
    /// 写入一条 Serilog 日志。
    /// </summary>
    /// <param name="logEvent">日志事件。</param>
    public void Emit(LogEvent logEvent)
    {
        // 统一转换为 UI 可消费的日志模型
        var entry = new LogEntry
        {
            Timestamp = logEvent.Timestamp,
            Level = logEvent.Level.ToString(),
            Message = logEvent.RenderMessage(),
            Exception = logEvent.Exception?.ToString()
        };

        _buffer.Add(entry);
    }
}
