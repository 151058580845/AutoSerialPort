using System;
using System.Collections.Generic;

namespace AutoSerialPort.Domain.Abstractions;

/// <summary>
/// 拆包解码器接口，负责将原始字节流切分为完整帧。
/// </summary>
public interface IFrameDecoder
{
    /// <summary>
    /// 解码器名称，用于界面显示与匹配配置。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 将串口原始数据拆分为完整帧，解决粘包/半包问题。
    /// </summary>
    IReadOnlyList<byte[]> Decode(ReadOnlySpan<byte> data);

    /// <summary>
    /// 重置内部缓冲区，通常在重连或切换配置时调用。
    /// </summary>
    void Reset();
}
