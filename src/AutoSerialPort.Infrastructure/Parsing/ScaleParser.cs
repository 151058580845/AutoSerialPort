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
/// 电子秤解析器，占位实现，后续可替换为真实协议解析。
/// </summary>
public class ScaleParser : IParser
{
    private readonly ScaleParserOptions _options;
    private readonly Encoding _encoding;

    /// <summary>
    /// 创建电子秤解析器。
    /// </summary>
    /// <param name="options">解析参数。</param>
    public ScaleParser(ScaleParserOptions options)
    {
        _options = options;
        _encoding = ResolveEncoding(options.Encoding);
    }

    /// <summary>
    /// 解析器名称。
    /// </summary>
    public string Name => "电子秤解析";

    /// <summary>
    /// 解析一帧电子秤数据。
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

        // 电子秤协议通常是整帧数据，后续可以在这里替换为真实解析规则
        var text = _encoding.GetString(buffer, 0, length);
        if (_options.TrimWhitespace)
        {
            text = text.Trim();
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            results.Add(new ParsedMessage
            {
                Text = text,
                Raw = buffer.AsSpan(0, length).ToArray(),
                Timestamp = DateTimeOffset.Now
            });
        }

        return Task.FromResult<IReadOnlyList<ParsedMessage>>(results);
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
