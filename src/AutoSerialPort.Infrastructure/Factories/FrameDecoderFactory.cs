using System;
using System.Collections.Generic;
using System.Linq;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Entities;
using Serilog;

namespace AutoSerialPort.Infrastructure.Factories;

/// <summary>
/// 拆包解码器工厂，根据配置创建解码器实例。
/// </summary>
public class FrameDecoderFactory
{
    private readonly Dictionary<string, IFrameDecoderProvider> _providers;
    private readonly IFrameDecoderProvider _fallback;

    public FrameDecoderFactory(IEnumerable<IFrameDecoderProvider> providers)
    {
        _providers = providers.ToDictionary(x => x.Type, StringComparer.OrdinalIgnoreCase);
        _fallback = _providers.TryGetValue("DelimiterFrameDecoder", out var defaultProvider)
            ? defaultProvider
            : _providers.Values.First();
    }

    /// <summary>
    /// 根据配置创建拆包解码器。
    /// </summary>
    /// <param name="config">解码器配置。</param>
    public IFrameDecoder Create(FrameDecoderConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.DecoderType) && _providers.TryGetValue(config.DecoderType, out var provider))
        {
            return provider.Create(config.ParametersJson);
        }

        // 兜底使用默认解码器
        Log.Warning("Unknown frame decoder type: {DecoderType}, falling back to {Fallback}", config.DecoderType, _fallback.Type);
        return _fallback.Create(_fallback.DefaultParametersJson);
    }
}
