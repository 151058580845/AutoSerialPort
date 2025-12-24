using System;
using System.Collections.Generic;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Models;

namespace AutoSerialPort.Infrastructure.Logging;

/// <summary>
/// 内存日志缓冲区实现，用于提供 UI 最近日志。
/// </summary>
public class LogBuffer : ILogBuffer
{
    private readonly object _lock = new();
    private readonly Queue<LogEntry> _entries;

    /// <summary>
    /// 创建日志缓冲区。
    /// </summary>
    /// <param name="capacity">最大容量。</param>
    public LogBuffer(int capacity)
    {
        Capacity = capacity;
        _entries = new Queue<LogEntry>(capacity);
    }

    /// <summary>
    /// 新日志写入事件。
    /// </summary>
    public event Action<LogEntry>? LogAdded;

    /// <summary>
    /// 缓冲区容量。
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    /// 获取当前快照。
    /// </summary>
    public IReadOnlyList<LogEntry> GetSnapshot()
    {
        lock (_lock)
        {
            return _entries.ToArray();
        }
    }

    /// <summary>
    /// 写入一条日志。
    /// </summary>
    /// <param name="entry">日志条目。</param>
    public void Add(LogEntry entry)
    {
        lock (_lock)
        {
            // 超出容量则丢弃最早日志
            while (_entries.Count >= Capacity)
            {
                _entries.Dequeue();
            }

            _entries.Enqueue(entry);
        }

        // 在锁外触发事件，避免阻塞
        LogAdded?.Invoke(entry);
    }
}
