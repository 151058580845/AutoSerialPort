using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AutoSerialPort.Application.Abstractions;
using AutoSerialPort.Application.Models;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Entities;
using AutoSerialPort.Domain.Options;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoSerialPort.UI.ViewModels;

/// <summary>
/// 解析与拆包设置视图模型，管理解析器与拆包器参数。
/// </summary>
public partial class ParserSettingsViewModel : ObservableObject, IParserSettingsViewModel
{
    private readonly Dictionary<string, IParserProvider> _parserProviders;
    private readonly Dictionary<string, IFrameDecoderProvider> _decoderProviders;
    private long _configId;
    private long _frameConfigId;
    private long _profileId;
    private long _deviceId;
    private string _parserType = "LineParser";
    private string _frameDecoderType = "DelimiterFrameDecoder";
    private bool _suppressTypeChange;
    private bool _suppressDecoderChange;

    /// <summary>
    /// JSON 序列化选项，使用 camelCase 命名策略以匹配数据库中的格式。
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public IReadOnlyList<AutoSerialPort.Application.Models.OptionItem> ParserTypeOptions { get; }
    public IReadOnlyList<AutoSerialPort.Application.Models.OptionItem> FrameDecoderTypeOptions { get; }

    [ObservableProperty]
    private AutoSerialPort.Application.Models.OptionItem? _selectedParserOption;

    [ObservableProperty]
    private AutoSerialPort.Application.Models.OptionItem? _selectedFrameDecoderOption;

    [ObservableProperty]
    private bool _isLineParser;

    [ObservableProperty]
    private bool _isJsonFieldParser;

    [ObservableProperty]
    private bool _isScaleParser;

    [ObservableProperty]
    private bool _isBarcodeParser;

    [ObservableProperty]
    private bool _isDelimiterFrameDecoder;

    [ObservableProperty]
    private bool _isHeaderFooterFrameDecoder;

    [ObservableProperty]
    private bool _isFixedLengthFrameDecoder;

    [ObservableProperty]
    private bool _isNoFrameDecoder;

    [ObservableProperty]
    private bool _showFrameDecoderSettings = true;

    [ObservableProperty]
    private string _encoding = "utf-8";

    [ObservableProperty]
    private string _separator = "\n";

    [ObservableProperty]
    private string _jsonFieldPath = "data";

    [ObservableProperty]
    private bool _jsonAllowLoose = true;

    [ObservableProperty]
    private bool _trimWhitespace = true;

    [ObservableProperty]
    private string _frameEncoding = "utf-8";

    [ObservableProperty]
    private string _frameDelimiter = "\n";

    [ObservableProperty]
    private bool _frameIncludeDelimiter = true;

    [ObservableProperty]
    private string _frameHeader = string.Empty;

    [ObservableProperty]
    private string _frameFooter = string.Empty;

    [ObservableProperty]
    private bool _frameIncludeHeaderFooter;

    [ObservableProperty]
    private int _frameLength = 16;

    [ObservableProperty]
    private int _frameMaxBufferLength = 65536;

    /// <summary>
    /// 创建解析设置视图模型。
    /// </summary>
    /// <param name="providers">解析器提供者集合。</param>
    /// <param name="decoderProviders">拆包器提供者集合。</param>
    public ParserSettingsViewModel(IEnumerable<IParserProvider> providers, IEnumerable<IFrameDecoderProvider> decoderProviders)
    {
        var parserList = providers.ToList();
        _parserProviders = parserList.ToDictionary(x => x.Type, StringComparer.OrdinalIgnoreCase);
        ParserTypeOptions = parserList.Select(x => new AutoSerialPort.Application.Models.OptionItem(x.Type, x.DisplayName)).ToList();
        SelectedParserOption = ParserTypeOptions.FirstOrDefault();

        var decoderList = decoderProviders.ToList();
        _decoderProviders = decoderList.ToDictionary(x => x.Type, StringComparer.OrdinalIgnoreCase);
        FrameDecoderTypeOptions = decoderList.Select(x => new AutoSerialPort.Application.Models.OptionItem(x.Type, x.DisplayName)).ToList();
        SelectedFrameDecoderOption = FrameDecoderTypeOptions.FirstOrDefault();
    }

    /// <summary>
    /// 加载解析与拆包配置。
    /// </summary>
    /// <param name="config">解析配置。</param>
    /// <param name="decoderConfig">拆包配置。</param>
    public void Load(ParserConfig config, FrameDecoderConfig decoderConfig)
    {
        _configId = config.Id;
        _frameConfigId = decoderConfig.Id;
        _profileId = config.ProfileId;
        _deviceId = config.DeviceId;
        _parserType = string.IsNullOrWhiteSpace(config.ParserType) ? "LineParser" : config.ParserType;
        _frameDecoderType = string.IsNullOrWhiteSpace(decoderConfig.DecoderType) ? "DelimiterFrameDecoder" : decoderConfig.DecoderType;

        // 避免加载时触发重复事件
        _suppressTypeChange = true;
        SelectedParserOption = ParserTypeOptions.FirstOrDefault(x =>
            string.Equals(x.Value, _parserType, StringComparison.OrdinalIgnoreCase)) ?? ParserTypeOptions.FirstOrDefault();
        ApplyParserType(_parserType, config.ParametersJson);
        _suppressTypeChange = false;

        // 避免加载时触发重复事件
        _suppressDecoderChange = true;
        SelectedFrameDecoderOption = FrameDecoderTypeOptions.FirstOrDefault(x =>
            string.Equals(x.Value, _frameDecoderType, StringComparison.OrdinalIgnoreCase)) ?? FrameDecoderTypeOptions.FirstOrDefault();
        ApplyFrameDecoderType(_frameDecoderType, decoderConfig.ParametersJson);
        _suppressDecoderChange = false;
    }

    /// <summary>
    /// 构建解析器配置。
    /// </summary>
    public ParserConfig BuildConfig()
    {
        var parametersJson = BuildParserParametersJson();

        return new ParserConfig
        {
            Id = _configId,
            ProfileId = _profileId,
            DeviceId = _deviceId,
            ParserType = _parserType,
            ParametersJson = parametersJson
        };
    }

    /// <summary>
    /// 构建拆包配置。
    /// </summary>
    public FrameDecoderConfig BuildFrameDecoderConfig()
    {
        var parametersJson = BuildFrameDecoderParametersJson();

        return new FrameDecoderConfig
        {
            Id = _frameConfigId,
            ProfileId = _profileId,
            DeviceId = _deviceId,
            DecoderType = _frameDecoderType,
            ParametersJson = parametersJson
        };
    }

    /// <summary>
    /// 处理解析器类型切换。
    /// </summary>
    partial void OnSelectedParserOptionChanged(AutoSerialPort.Application.Models.OptionItem? value)
    {
        if (value == null || _suppressTypeChange)
        {
            return;
        }

        _parserType = value.Value;
        ApplyParserType(_parserType, GetDefaultParserParametersJson(_parserType));
    }

    /// <summary>
    /// 处理拆包器类型切换。
    /// </summary>
    partial void OnSelectedFrameDecoderOptionChanged(AutoSerialPort.Application.Models.OptionItem? value)
    {
        if (value == null || _suppressDecoderChange)
        {
            return;
        }

        _frameDecoderType = value.Value;
        ApplyFrameDecoderType(_frameDecoderType, GetDefaultFrameDecoderParametersJson(_frameDecoderType));
    }

    /// <summary>
    /// 根据解析器类型加载参数到界面。
    /// </summary>
    private void ApplyParserType(string parserType, string? parametersJson)
    {
        IsLineParser = string.Equals(parserType, "LineParser", StringComparison.OrdinalIgnoreCase);
        IsJsonFieldParser = string.Equals(parserType, "JsonFieldParser", StringComparison.OrdinalIgnoreCase);
        IsScaleParser = string.Equals(parserType, "ScaleParser", StringComparison.OrdinalIgnoreCase);
        IsBarcodeParser = string.Equals(parserType, "BarcodeParser", StringComparison.OrdinalIgnoreCase);

        if (IsLineParser)
        {
            var options = Deserialize(parametersJson, new LineParserOptions());
            Encoding = options.Encoding;
            Separator = options.Separator;
            return;
        }

        if (IsJsonFieldParser)
        {
            var options = Deserialize(parametersJson, new JsonFieldParserOptions());
            Encoding = options.Encoding;
            Separator = options.Separator;
            JsonFieldPath = options.FieldPath;
            JsonAllowLoose = options.AllowLooseJson;
            return;
        }

        if (IsScaleParser)
        {
            var options = Deserialize(parametersJson, new ScaleParserOptions());
            Encoding = options.Encoding;
            TrimWhitespace = options.TrimWhitespace;
            return;
        }

        if (IsBarcodeParser)
        {
            var options = Deserialize(parametersJson, new BarcodeParserOptions());
            Encoding = options.Encoding;
            TrimWhitespace = options.TrimWhitespace;
        }
    }

    /// <summary>
    /// 根据拆包类型加载参数到界面。
    /// </summary>
    private void ApplyFrameDecoderType(string decoderType, string? parametersJson)
    {
        IsNoFrameDecoder = string.Equals(decoderType, "NoFrameDecoder", StringComparison.OrdinalIgnoreCase);
        IsDelimiterFrameDecoder = string.Equals(decoderType, "DelimiterFrameDecoder", StringComparison.OrdinalIgnoreCase);
        IsHeaderFooterFrameDecoder = string.Equals(decoderType, "HeaderFooterFrameDecoder", StringComparison.OrdinalIgnoreCase);
        IsFixedLengthFrameDecoder = string.Equals(decoderType, "FixedLengthFrameDecoder", StringComparison.OrdinalIgnoreCase);
        ShowFrameDecoderSettings = !IsNoFrameDecoder;

        if (IsNoFrameDecoder)
        {
            return;
        }

        // 按拆包类型加载对应配置，便于界面实时切换
        if (IsDelimiterFrameDecoder)
        {
            var options = Deserialize(parametersJson, new DelimiterFrameDecoderOptions());
            FrameEncoding = options.Encoding;
            FrameDelimiter = options.Delimiter;
            FrameIncludeDelimiter = options.IncludeDelimiter;
            FrameMaxBufferLength = options.MaxBufferLength;
            return;
        }

        if (IsHeaderFooterFrameDecoder)
        {
            var options = Deserialize(parametersJson, new HeaderFooterFrameDecoderOptions());
            FrameEncoding = options.Encoding;
            FrameHeader = options.Header;
            FrameFooter = options.Footer;
            FrameIncludeHeaderFooter = options.IncludeHeaderFooter;
            FrameMaxBufferLength = options.MaxBufferLength;
            return;
        }

        if (IsFixedLengthFrameDecoder)
        {
            var options = Deserialize(parametersJson, new FixedLengthFrameDecoderOptions());
            FrameLength = options.FrameLength;
            FrameMaxBufferLength = options.MaxBufferLength;
        }
    }

    /// <summary>
    /// 将当前解析设置序列化为 JSON。
    /// </summary>
    private string BuildParserParametersJson()
    {
        if (IsJsonFieldParser)
        {
            return JsonSerializer.Serialize(new JsonFieldParserOptions
            {
                Encoding = Encoding,
                Separator = Separator,
                FieldPath = JsonFieldPath,
                AllowLooseJson = JsonAllowLoose
            }, JsonOptions);
        }

        if (IsScaleParser)
        {
            return JsonSerializer.Serialize(new ScaleParserOptions
            {
                Encoding = Encoding,
                TrimWhitespace = TrimWhitespace
            }, JsonOptions);
        }

        if (IsBarcodeParser)
        {
            return JsonSerializer.Serialize(new BarcodeParserOptions
            {
                Encoding = Encoding,
                TrimWhitespace = TrimWhitespace
            }, JsonOptions);
        }

        return JsonSerializer.Serialize(new LineParserOptions
        {
            Encoding = Encoding,
            Separator = Separator
        }, JsonOptions);
    }

    /// <summary>
    /// 将当前拆包设置序列化为 JSON。
    /// </summary>
    private string BuildFrameDecoderParametersJson()
    {
        if (IsNoFrameDecoder)
        {
            return "{}";
        }

        // 将当前界面设置序列化为拆包参数
        if (IsHeaderFooterFrameDecoder)
        {
            return JsonSerializer.Serialize(new HeaderFooterFrameDecoderOptions
            {
                Encoding = FrameEncoding,
                Header = FrameHeader,
                Footer = FrameFooter,
                IncludeHeaderFooter = FrameIncludeHeaderFooter,
                MaxBufferLength = FrameMaxBufferLength
            }, JsonOptions);
        }

        if (IsFixedLengthFrameDecoder)
        {
            return JsonSerializer.Serialize(new FixedLengthFrameDecoderOptions
            {
                FrameLength = FrameLength,
                MaxBufferLength = FrameMaxBufferLength
            }, JsonOptions);
        }

        return JsonSerializer.Serialize(new DelimiterFrameDecoderOptions
        {
            Encoding = FrameEncoding,
            Delimiter = FrameDelimiter,
            IncludeDelimiter = FrameIncludeDelimiter,
            MaxBufferLength = FrameMaxBufferLength
        }, JsonOptions);
    }

    /// <summary>
    /// 获取解析器默认参数 JSON。
    /// </summary>
    private string GetDefaultParserParametersJson(string parserType)
    {
        if (_parserProviders.TryGetValue(parserType, out var provider))
        {
            return provider.DefaultParametersJson;
        }

        return _parserProviders.Values.First().DefaultParametersJson;
    }

    /// <summary>
    /// 获取拆包器默认参数 JSON。
    /// </summary>
    private string GetDefaultFrameDecoderParametersJson(string decoderType)
    {
        if (_decoderProviders.TryGetValue(decoderType, out var provider))
        {
            return provider.DefaultParametersJson;
        }

        return _decoderProviders.Values.First().DefaultParametersJson;
    }

    /// <summary>
    /// 反序列化 JSON 参数，失败则回退到默认值。
    /// </summary>
    private static T Deserialize<T>(string? json, T fallback) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return fallback;
        }

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
