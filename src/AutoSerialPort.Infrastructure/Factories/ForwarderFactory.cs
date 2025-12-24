using System;
using System.Collections.Generic;
using System.Text.Json;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Entities;
using AutoSerialPort.Domain.Options;
using AutoSerialPort.Infrastructure.Forwarders;
using Serilog;

namespace AutoSerialPort.Infrastructure.Factories;

/// <summary>
/// 转发器工厂，根据配置创建转发器实例。
/// </summary>
public class ForwarderFactory
{
    private readonly IClipboardService _clipboardService;
    private readonly ITypingService _typingService;

    /// <summary>
    /// JSON 序列化选项，使用 camelCase 命名策略以匹配数据库中的格式。
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ForwarderFactory(IClipboardService clipboardService, ITypingService typingService)
    {
        _clipboardService = clipboardService;
        _typingService = typingService;
    }

    /// <summary>
    /// 批量创建转发器列表。
    /// </summary>
    /// <param name="configs">转发器配置集合。</param>
    public IReadOnlyList<IForwarder> Create(ForwarderConfig[] configs)
    {
        var list = new List<IForwarder>();
        foreach (var config in configs)
        {
            var forwarder = CreateSingle(config);
            if (forwarder != null)
            {
                list.Add(forwarder);
            }
        }

        return list;
    }

    /// <summary>
    /// 创建单个转发器。
    /// </summary>
    /// <param name="config">转发器配置。</param>
    private IForwarder? CreateSingle(ForwarderConfig config)
    {
        if (string.Equals(config.ForwarderType, "TcpForwarder", StringComparison.OrdinalIgnoreCase))
        {
            var options = Deserialize(config.ParametersJson, new TcpForwarderOptions());
            return new TcpForwarder(options, config.IsEnabled);
        }

        if (string.Equals(config.ForwarderType, "MqttForwarder", StringComparison.OrdinalIgnoreCase))
        {
            var options = Deserialize(config.ParametersJson, new MqttForwarderOptions());
            return new MqttForwarder(options, config.IsEnabled);
        }

        if (string.Equals(config.ForwarderType, "ClipboardForwarder", StringComparison.OrdinalIgnoreCase))
        {
            var options = Deserialize(config.ParametersJson, new ClipboardForwarderOptions());
            return new ClipboardForwarder(_clipboardService, options, config.IsEnabled);
        }

        if (string.Equals(config.ForwarderType, "TypingForwarder", StringComparison.OrdinalIgnoreCase))
        {
            var options = Deserialize(config.ParametersJson, new TypingForwarderOptions());
            return new TypingForwarder(_typingService, options, config.IsEnabled);
        }

        Log.Warning("Unknown forwarder type: {ForwarderType}", config.ForwarderType);
        return null;
    }

    /// <summary>
    /// 反序列化 JSON 参数，失败则回退为默认值。
    /// </summary>
    /// <param name="json">JSON 字符串。</param>
    /// <param name="fallback">默认值。</param>
    private static T Deserialize<T>(string json, T fallback) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }
}
