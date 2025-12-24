using System.Collections.Generic;
using AutoSerialPort.Application.Models;

namespace AutoSerialPort.Application.Abstractions;

/// <summary>
/// 解析器设置视图模型接口
/// 提供解析器设置的抽象表示，避免Application层直接依赖UI层
/// </summary>
public interface IParserSettingsViewModel
{
    /// <summary>
    /// 解析器类型选项
    /// </summary>
    IReadOnlyList<OptionItem> ParserTypeOptions { get; }

    /// <summary>
    /// 拆包器类型选项
    /// </summary>
    IReadOnlyList<OptionItem> FrameDecoderTypeOptions { get; }

    /// <summary>
    /// 选中的解析器选项
    /// </summary>
    OptionItem? SelectedParserOption { get; set; }

    /// <summary>
    /// 选中的拆包器选项
    /// </summary>
    OptionItem? SelectedFrameDecoderOption { get; set; }

    // 解析器相关属性
    /// <summary>
    /// 编码
    /// </summary>
    string Encoding { get; set; }

    /// <summary>
    /// 分隔符
    /// </summary>
    string Separator { get; set; }

    /// <summary>
    /// JSON字段路径
    /// </summary>
    string JsonFieldPath { get; set; }

    /// <summary>
    /// 是否允许宽松JSON
    /// </summary>
    bool JsonAllowLoose { get; set; }

    /// <summary>
    /// 是否去除首尾空白
    /// </summary>
    bool TrimWhitespace { get; set; }

    // 解析器类型判断属性
    /// <summary>
    /// 是否为行解析器
    /// </summary>
    bool IsLineParser { get; }

    /// <summary>
    /// 是否为JSON字段解析器
    /// </summary>
    bool IsJsonFieldParser { get; }

    /// <summary>
    /// 是否为称重解析器
    /// </summary>
    bool IsScaleParser { get; }

    /// <summary>
    /// 是否为条码解析器
    /// </summary>
    bool IsBarcodeParser { get; }

    // 拆包器相关属性
    /// <summary>
    /// 拆包编码
    /// </summary>
    string FrameEncoding { get; set; }

    /// <summary>
    /// 拆包分隔符
    /// </summary>
    string FrameDelimiter { get; set; }

    /// <summary>
    /// 是否包含分隔符
    /// </summary>
    bool FrameIncludeDelimiter { get; set; }

    /// <summary>
    /// 帧头
    /// </summary>
    string FrameHeader { get; set; }

    /// <summary>
    /// 帧尾
    /// </summary>
    string FrameFooter { get; set; }

    /// <summary>
    /// 是否包含帧头帧尾
    /// </summary>
    bool FrameIncludeHeaderFooter { get; set; }

    /// <summary>
    /// 固定长度
    /// </summary>
    int FrameLength { get; set; }

    /// <summary>
    /// 最大缓存长度
    /// </summary>
    int FrameMaxBufferLength { get; set; }

    // 拆包器类型判断属性
    /// <summary>
    /// 是否为分隔符拆包器
    /// </summary>
    bool IsDelimiterFrameDecoder { get; }

    /// <summary>
    /// 是否为帧头帧尾拆包器
    /// </summary>
    bool IsHeaderFooterFrameDecoder { get; }

    /// <summary>
    /// 是否为固定长度拆包器
    /// </summary>
    bool IsFixedLengthFrameDecoder { get; }

    /// <summary>
    /// 是否显示拆包器设置
    /// </summary>
    bool ShowFrameDecoderSettings { get; }
}