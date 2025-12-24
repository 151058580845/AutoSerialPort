using System;
using System.Collections.Generic;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Options;

namespace AutoSerialPort.Infrastructure.Framing;

/// <summary>
/// 分隔符拆包解码器，按指定分隔符切分完整帧。
/// </summary>
public class DelimiterFrameDecoder : IFrameDecoder
{
    private readonly DelimiterFrameDecoderOptions _options;
    private readonly List<byte> _buffer = new();
    private readonly byte[] _delimiterBytes;

    /// <summary>
    /// 创建分隔符拆包解码器。
    /// </summary>
    /// <param name="options">配置参数。</param>
    public DelimiterFrameDecoder(DelimiterFrameDecoderOptions options)
    {
        _options = options;
        var encoding = FrameDecoderHelpers.ResolveEncoding(options.Encoding);
        _delimiterBytes = encoding.GetBytes(options.Delimiter ?? string.Empty);
    }

    /// <summary>
    /// 解码器名称。
    /// </summary>
    public string Name => "分隔符拆包";

    /// <summary>
    /// 按分隔符切分完整帧。
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
        FrameDecoderHelpers.TrimBuffer(_buffer, _options.MaxBufferLength, _delimiterBytes.Length * 2);

        var frames = new List<byte[]>();
        while (true)
        {
            var index = FrameDecoderHelpers.IndexOf(_buffer, _delimiterBytes);
            if (index < 0)
            {
                break;
            }

            // 根据配置决定是否包含分隔符
            var payloadLength = _options.IncludeDelimiter ? index + _delimiterBytes.Length : index;
            var frame = _buffer.GetRange(0, payloadLength).ToArray();
            frames.Add(frame);

            // 移除已消费的数据，继续寻找下一帧
            _buffer.RemoveRange(0, index + _delimiterBytes.Length);
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
