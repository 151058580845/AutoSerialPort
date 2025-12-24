using System;
using System.Collections.Generic;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Options;

namespace AutoSerialPort.Infrastructure.Framing;

/// <summary>
/// 固定长度拆包解码器，按固定字节数切分帧。
/// </summary>
public class FixedLengthFrameDecoder : IFrameDecoder
{
    private readonly FixedLengthFrameDecoderOptions _options;
    private readonly List<byte> _buffer = new();

    /// <summary>
    /// 创建固定长度拆包解码器。
    /// </summary>
    /// <param name="options">配置参数。</param>
    public FixedLengthFrameDecoder(FixedLengthFrameDecoderOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// 解码器名称。
    /// </summary>
    public string Name => "固定长度拆包";

    /// <summary>
    /// 按固定长度切分完整帧。
    /// </summary>
    /// <param name="data">原始数据片段。</param>
    public IReadOnlyList<byte[]> Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
        {
            return Array.Empty<byte[]>();
        }

        // 追加到内部缓冲，处理粘包/半包
        _buffer.AddRange(data.ToArray());
        FrameDecoderHelpers.TrimBuffer(_buffer, _options.MaxBufferLength, _options.FrameLength);

        if (_options.FrameLength <= 0)
        {
            return Array.Empty<byte[]>();
        }

        var frames = new List<byte[]>();
        while (_buffer.Count >= _options.FrameLength)
        {
            var frame = _buffer.GetRange(0, _options.FrameLength).ToArray();
            frames.Add(frame);
            _buffer.RemoveRange(0, _options.FrameLength);
        }

        return frames;
    }

    /// <summary>
    /// 清空内部缓冲。
    /// </summary>
    public void Reset()
    {
        _buffer.Clear();
    }
}
