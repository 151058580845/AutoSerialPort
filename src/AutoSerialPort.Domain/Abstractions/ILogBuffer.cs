using System;
using System.Collections.Generic;
using AutoSerialPort.Domain.Models;

namespace AutoSerialPort.Domain.Abstractions;

/// <summary>
/// 日志缓冲区接口，用于向 UI 提供最近日志。
/// </summary>
public interface ILogBuffer
{
    /// <summary>
    /// 新日志写入时触发。
    /// </summary>
    event Action<LogEntry>? LogAdded;

    /// <summary>
    /// 缓冲区容量。
    /// </summary>
    int Capacity { get; }

    /// <summary>
    /// 获取当前日志快照。
    /// </summary>
    IReadOnlyList<LogEntry> GetSnapshot();

    /// <summary>
    /// 写入一条日志。
    /// </summary>
    /// <param name="entry">日志条目。</param>
    void Add(LogEntry entry);
}
