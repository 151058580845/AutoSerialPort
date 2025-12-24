using System.Threading;
using System.Threading.Tasks;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Models;
using AutoSerialPort.Domain.Options;
using Serilog;

namespace AutoSerialPort.Infrastructure.Forwarders;

/// <summary>
/// 模拟输入转发器，通过系统输入服务输出文本。
/// </summary>
public class TypingForwarder : ForwarderBase
{
    private readonly ITypingService _typingService;
    private readonly TypingForwarderOptions _options;

    /// <summary>
    /// 创建模拟输入转发器。
    /// </summary>
    /// <param name="typingService">输入服务。</param>
    /// <param name="options">转发配置。</param>
    /// <param name="enabled">是否启用。</param>
    public TypingForwarder(ITypingService typingService, TypingForwarderOptions options, bool enabled)
    {
        _typingService = typingService;
        _options = options;
        IsEnabled = enabled;
    }

    /// <summary>
    /// 转发器名称。
    /// </summary>
    public override string Name => "模拟输入";

    /// <summary>
    /// 模拟输入消息内容。
    /// </summary>
    /// <param name="message">解析消息。</param>
    /// <param name="ct">取消令牌。</param>
    public override async Task ForwardAsync(ParsedMessage message, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            return;
        }

        if (!_typingService.IsAvailable)
        {
            // 平台不支持时给出提示
            LogWarning(_typingService.GetUnavailableReason() ?? "Typing service not available");
            return;
        }

        try
        {
            // 支持可选延迟，避免目标程序卡顿
            if (_options.DelayMs > 0)
            {
                await Task.Delay(_options.DelayMs, ct);
            }

            var ok = await _typingService.TryTypeAsync(message.Text, ct);
            if (!ok)
            {
                LogWarning("Typing forwarder failed to type text");
            }
        }
        catch (System.Exception ex)
        {
            LogError(ex, "Typing forwarder failed");
        }
    }
}
