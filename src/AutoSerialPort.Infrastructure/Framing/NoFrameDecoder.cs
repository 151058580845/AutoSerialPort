using System;
using System.Collections.Generic;
using AutoSerialPort.Domain.Abstractions;

namespace AutoSerialPort.Infrastructure.Framing;

/// <summary>
/// 不拆包解码器，直接将读取到的数据作为完整帧输出。
/// </summary>
public class NoFrameDecoder : IFrameDecoder
{
    /// <summary>
    /// 解码器名称。
    /// </summary>
    public string Name => "不拆包";

    /// <summary>
    /// 不做拆包处理，直接返回当前数据。
    /// </summary>
    public IReadOnlyList<byte[]> Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
        {
            return Array.Empty<byte[]>();
        }

        return new[] { data.ToArray() };
    }

    /// <summary>
    /// 无内部状态需要重置。
    /// </summary>
    public void Reset()
    {
    }
}
