using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Models;
using AutoSerialPort.Domain.Options;

namespace AutoSerialPort.Infrastructure.Parsing;

/// <summary>
/// JSON 字段解析器，从 JSON 文本中提取指定字段。
/// </summary>
public class JsonFieldParser : IParser
{
    private readonly JsonFieldParserOptions _options;
    private readonly List<byte> _buffer = new();
    private readonly byte[] _separatorBytes;
    private readonly Encoding _encoding;

    /// <summary>
    /// 创建 JSON 字段解析器。
    /// </summary>
    /// <param name="options">解析参数。</param>
    public JsonFieldParser(JsonFieldParserOptions options)
    {
        _options = options;
        _encoding = ResolveEncoding(options.Encoding);
        _separatorBytes = _encoding.GetBytes(options.Separator);
    }

    /// <summary>
    /// 解析器名称。
    /// </summary>
    public string Name => "JSON字段解析";

    /// <summary>
    /// 解析一段数据并提取字段内容。
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
            if (!TryExtractValue(text, out var valueText))
            {
                continue;
            }

            // 只转发目标字段内容
            results.Add(new ParsedMessage
            {
                Text = valueText,
                Raw = lineBytes,
                Timestamp = DateTimeOffset.Now
            });
        }

        return Task.FromResult<IReadOnlyList<ParsedMessage>>(results);
    }

    /// <summary>
    /// 从 JSON 文本中提取字段值。
    /// </summary>
    /// <param name="input">JSON 文本。</param>
    /// <param name="valueText">输出字段文本。</param>
    private bool TryExtractValue(string input, out string valueText)
    {
        valueText = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        // 可选的宽松解析，自动补全常见格式问题
        var json = _options.AllowLooseJson ? NormalizeJson(input) : input;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var element = doc.RootElement;
            foreach (var segment in SplitPath(_options.FieldPath))
            {
                if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(segment, out var child))
                {
                    return false;
                }

                element = child;
            }

            valueText = element.ValueKind == JsonValueKind.String
                ? element.GetString() ?? string.Empty
                : element.GetRawText();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 以点号分割字段路径。
    /// </summary>
    /// <param name="path">字段路径。</param>
    private static IEnumerable<string> SplitPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Array.Empty<string>();
        }

        return path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// 将松散格式 JSON 规范化。
    /// </summary>
    /// <param name="input">原始文本。</param>
    private static string NormalizeJson(string input)
    {
        var trimmed = input.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return trimmed;
        }

        // 常见处理：单引号替换与字段名补引号
        var normalized = trimmed.Replace("'", "\"");
        return Regex.Replace(normalized, "(?<=[{,]\\s*)([A-Za-z_][A-Za-z0-9_]*)(?=\\s*:)", "\"$1\"");
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
