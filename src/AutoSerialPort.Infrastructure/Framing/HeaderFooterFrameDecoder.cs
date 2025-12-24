using System;
using System.Collections.Generic;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Options;

namespace AutoSerialPort.Infrastructure.Framing;

/// <summary>
/// 帧头帧尾拆包解码器，按帧头与帧尾切分完整帧。
/// </summary>
public class HeaderFooterFrameDecoder : IFrameDecoder
{
    private readonly HeaderFooterFrameDecoderOptions _options;
    private readonly List<byte> _buffer = new();
    private readonly byte[] _headerBytes;
    private readonly byte[] _footerBytes;

    /// <summary>
    /// 创建帧头帧尾拆包解码器。
    /// </summary>
    /// <param name="options">配置参数。</param>
    public HeaderFooterFrameDecoder(HeaderFooterFrameDecoderOptions options)
    {
        _options = options;
        var encoding = FrameDecoderHelpers.ResolveEncoding(options.Encoding);
        _headerBytes = encoding.GetBytes(options.Header ?? string.Empty);
        _footerBytes = encoding.GetBytes(options.Footer ?? string.Empty);
    }

    /// <summary>
    /// 解码器名称。
    /// </summary>
    public string Name => "帧头帧尾拆包";

    /// <summary>
    /// 按帧头/帧尾切分完整帧。
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
        FrameDecoderHelpers.TrimBuffer(_buffer, _options.MaxBufferLength, _headerBytes.Length + _footerBytes.Length);

        if (_headerBytes.Length == 0 || _footerBytes.Length == 0)
        {
            // 帧头/帧尾为空时无法拆包
            return Array.Empty<byte[]>();
        }

        var frames = new List<byte[]>();
        while (true)
        {
            var headerIndex = FrameDecoderHelpers.IndexOf(_buffer, _headerBytes);
            if (headerIndex < 0)
            {
                // 未找到帧头时保留尾部，避免帧头跨包被截断
                FrameDecoderHelpers.TrimBuffer(_buffer, _options.MaxBufferLength, _headerBytes.Length - 1);
                break;
            }

            if (headerIndex > 0)
            {
                // 丢弃帧头之前的无效数据
                _buffer.RemoveRange(0, headerIndex);
            }

            var footerIndex = FrameDecoderHelpers.IndexOf(_buffer, _footerBytes, _headerBytes.Length);
            if (footerIndex < 0)
            {
                break;
            }

            // 根据配置决定是否包含帧头帧尾
            var frameStart = _options.IncludeHeaderFooter ? 0 : _headerBytes.Length;
            var frameLength = _options.IncludeHeaderFooter
                ? footerIndex + _footerBytes.Length
                : footerIndex - _headerBytes.Length;
            if (frameLength < 0)
            {
                _buffer.RemoveRange(0, footerIndex + _footerBytes.Length);
                continue;
            }

            var frame = _buffer.GetRange(frameStart, frameLength).ToArray();
            frames.Add(frame);

            // 消费已完成的一帧数据
            _buffer.RemoveRange(0, footerIndex + _footerBytes.Length);
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
