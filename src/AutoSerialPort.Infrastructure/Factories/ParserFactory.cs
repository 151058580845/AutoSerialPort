using System;
using System.Collections.Generic;
using System.Linq;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Entities;
using Serilog;

namespace AutoSerialPort.Infrastructure.Factories;

/// <summary>
/// 解析器工厂，根据配置创建对应解析器实例。
/// </summary>
public class ParserFactory
{
    private readonly Dictionary<string, IParserProvider> _providers;
    private readonly IParserProvider _fallback;

    public ParserFactory(IEnumerable<IParserProvider> providers)
    {
        _providers = providers.ToDictionary(x => x.Type, StringComparer.OrdinalIgnoreCase);
        _fallback = _providers.TryGetValue("LineParser", out var lineParser)
            ? lineParser
            : _providers.Values.First();
    }

    /// <summary>
    /// 根据配置创建解析器。
    /// </summary>
    /// <param name="config">解析器配置。</param>
    public IParser Create(ParserConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.ParserType) && _providers.TryGetValue(config.ParserType, out var provider))
        {
            return provider.Create(config.ParametersJson);
        }

        // 找不到匹配解析器时使用默认解析器兜底
        Log.Warning("Unknown parser type: {ParserType}, falling back to {Fallback}", config.ParserType, _fallback.Type);
        return _fallback.Create(_fallback.DefaultParametersJson);
    }
}
