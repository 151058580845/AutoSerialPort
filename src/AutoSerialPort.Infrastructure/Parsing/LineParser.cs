using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Models;
using AutoSerialPort.Domain.Options;

namespace AutoSerialPort.Infrastructure.Parsing;

/// <summary>
/// 行解析器，按分隔符拆分为多条文本消息。
/// </summary>
public class LineParser : IParser
{
    private readonly LineParserOptions _options;
    private readonly List<byte> _buffer = new();
    private readonly byte[] _separatorBytes;
    private readonly Encoding _encoding;

    /// <summary>
    /// 创建行解析器。
    /// </summary>
    /// <param name="options">解析参数。</param>
    public LineParser(LineParserOptions options)
    {
        _options = options;
        _encoding = ResolveEncoding(options.Encoding);
        _separatorBytes = _encoding.GetBytes(options.Separator);
    }

    /// <summary>
    /// 解析器名称。
    /// </summary>
    public string Name => "行解析";

    /// <summary>
    /// 解析一段数据并输出多条消息。
    /// </summary>
    /// <param name="buffer">输入缓冲区。</param>
    /// <param name="length">有效长度。</param>
    /// <param name="ct">取消令牌。</param>
    public Task<IReadOnlyList<ParsedMessage>> ParseAsync(byte[] buffer, int length, CancellationToken ct)
    {
        var results = new List<ParsedMessage>();
        if (length <= 0)
        {
            return Task.FromResult<IReadOnlyList<ParsedMessage>>(results);
        }

        // 追加到内部缓冲，处理半包
        _buffer.AddRange(buffer.AsSpan(0, length).ToArray());
        while (true)
        {
            var index = IndexOf(_buffer, _separatorBytes);
            if (index < 0)
            {
                break;
            }

            var lineBytes = _buffer.GetRange(0, index).ToArray();
            _buffer.RemoveRange(0, index + _separatorBytes.Length);

            var text = _encoding.GetString(lineBytes);
            results.Add(new ParsedMessage
            {
                Text = text,
                Raw = lineBytes,
                Timestamp = DateTimeOffset.Now
            });
        }

        return Task.FromResult<IReadOnlyList<ParsedMessage>>(results);
    }

    /// <summary>
    /// 在缓冲区内查找分隔符。
    /// </summary>
    private static int IndexOf(List<byte> buffer, byte[] pattern)
    {
        if (pattern.Length == 0 || buffer.Count < pattern.Length)
        {
            return -1;
        }

        for (var i = 0; i <= buffer.Count - pattern.Length; i++)
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
    /// 解析编码名称，失败时回退 UTF-8。
    /// </summary>
    /// <param name="encodingName">编码名称。</param>
    private static Encoding ResolveEncoding(string? encodingName)
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
}
