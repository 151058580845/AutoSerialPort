using System.Linq;
using System.Threading.Tasks;
using AutoSerialPort.Domain.Abstractions;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace AutoSerialPort.UI.Services;

/// <summary>
/// Avalonia 剪贴板服务实现。
/// </summary>
public class ClipboardService : IClipboardService
{
    /// <summary>
    /// 写入剪贴板文本。
    /// </summary>
    /// <param name="text">要写入的内容。</param>
    public Task SetTextAsync(string text)
    {
        // 获取主窗口剪贴板对象
        var lifetime = Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var window = lifetime?.MainWindow ?? lifetime?.Windows.FirstOrDefault();
        var clipboard = window?.Clipboard;

        if (clipboard == null)
        {
            return Task.CompletedTask;
        }

        // 切回 UI 线程调用
        return Dispatcher.UIThread.InvokeAsync(() => clipboard.SetTextAsync(text));
    }
}
