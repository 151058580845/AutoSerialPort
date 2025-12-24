using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoSerialPort.Domain.Models;

namespace AutoSerialPort.Domain.Abstractions;

/// <summary>
/// 解析器接口，负责将完整帧解析为业务消息。
/// </summary>
public interface IParser
{
    /// <summary>
    /// 解析器名称，用于界面展示与匹配配置。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 解析一帧数据。
    /// </summary>
    /// <param name="buffer">帧数据缓冲区。</param>
    /// <param name="length">有效长度。</param>
    /// <param name="ct">取消令牌。</param>
    Task<IReadOnlyList<ParsedMessage>> ParseAsync(byte[] buffer, int length, CancellationToken ct);
}
