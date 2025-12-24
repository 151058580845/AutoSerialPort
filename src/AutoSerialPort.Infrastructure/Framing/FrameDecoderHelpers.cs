using System;
using System.Collections.Generic;
using System.Text;

namespace AutoSerialPort.Infrastructure.Framing;

/// <summary>
/// 拆包相关的通用辅助方法。
/// </summary>
internal static class FrameDecoderHelpers
{
    /// <summary>
    /// 根据名称解析编码，失败时回退到 UTF-8。
    /// </summary>
    /// <param name="encodingName">编码名称。</param>
    public static Encoding ResolveEncoding(string? encodingName)
    {
        if (string.IsNullOrWhiteSpace(encodingName))
        {
            return Encoding.UTF8;
        }

        try
        {
            return Encoding.GetEncoding(encodingName);
        }
        catch
        {
            return Encoding.UTF8;
        }
    }

    /// <summary>
    /// 在缓冲区中查找字节序列的位置。
    /// </summary>
    /// <param name="buffer">缓冲区。</param>
    /// <param name="pattern">要匹配的字节序列。</param>
    /// <param name="startIndex">起始索引。</param>
    public static int IndexOf(List<byte> buffer, byte[] pattern, int startIndex = 0)
    {
        if (pattern.Length == 0 || buffer.Count < pattern.Length || startIndex < 0)
        {
            return -1;
        }

        for (var i = startIndex; i <= buffer.Count - pattern.Length; i++)
        {
            var match = true;
            for (var j = 0; j < pattern.Length; j++)
            {
                if (buffer[i + j] != pattern[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 截断缓冲区，防止无限增长。
    /// </summary>
    /// <param name="buffer">缓冲区。</param>
    /// <param name="maxLength">最大长度。</param>
    /// <param name="keepTailLength">保留尾部长度。</param>
    public static void TrimBuffer(List<byte> buffer, int maxLength, int keepTailLength = 0)
    {
        if (maxLength <= 0 || buffer.Count <= maxLength)
        {
            return;
        }

        var keep = Math.Clamp(keepTailLength, 0, maxLength);
        var removeCount = buffer.Count - keep;
        if (removeCount > 0)
        {
            // 超出缓存上限时只保留末尾部分，避免无限增长
            buffer.RemoveRange(0, removeCount);
        }
    }
}
