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
/// 扫码枪解析器，占位实现，后续可替换为真实规则解析。
/// </summary>
public class BarcodeParser : IParser
{
    private readonly BarcodeParserOptions _options;
    private readonly Encoding _encoding;

    /// <summary>
    /// 创建扫码枪解析器。
    /// </summary>
    /// <param name="options">解析参数。</param>
    public BarcodeParser(BarcodeParserOptions options)
    {
        _options = options;
        _encoding = ResolveEncoding(options.Encoding);
    }

    /// <summary>
    /// 解析器名称。
    /// </summary>
    public string Name => "扫码枪解析";

    /// <summary>
    /// 解析一帧扫码枪数据。
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

        // 扫码枪通常是一帧一条数据，后续可在此解析前缀/校验等规则
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
