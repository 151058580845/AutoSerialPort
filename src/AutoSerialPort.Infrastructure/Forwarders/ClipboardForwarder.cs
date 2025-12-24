using System.Threading;
using System.Threading.Tasks;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Models;
using AutoSerialPort.Domain.Options;

namespace AutoSerialPort.Infrastructure.Forwarders;

/// <summary>
/// 剪贴板转发器，将内容写入系统剪贴板。
/// </summary>
public class ClipboardForwarder : ForwarderBase
{
    private readonly IClipboardService _clipboardService;
    private readonly ClipboardForwarderOptions _options;

    /// <summary>
    /// 创建剪贴板转发器。
    /// </summary>
    /// <param name="clipboardService">剪贴板服务。</param>
    /// <param name="options">转发配置。</param>
    /// <param name="enabled">是否启用。</param>
    public ClipboardForwarder(IClipboardService clipboardService, ClipboardForwarderOptions options, bool enabled)
    {
        _clipboardService = clipboardService;
        _options = options;
        IsEnabled = enabled;
    }

    /// <summary>
    /// 转发器名称。
    /// </summary>
    public override string Name => "剪贴板";

    /// <summary>
    /// 将消息写入剪贴板。
    /// </summary>
    /// <param name="message">解析消息。</param>
    /// <param name="ct">取消令牌。</param>
    public override async Task ForwardAsync(ParsedMessage message, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            return;
        }

        // 可选追加换行，便于连续粘贴
        var text = _options.AppendNewLine ? message.Text + "\n" : message.Text;
        try
        {
            await _clipboardService.SetTextAsync(text);
        }
        catch (System.Exception ex)
        {
            LogError(ex, "Clipboard forwarder failed");
        }
    }
}
