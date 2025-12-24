using System.Collections.ObjectModel;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;

namespace AutoSerialPort.UI.ViewModels;

/// <summary>
/// 日志视图模型，提供最近日志与导出路径。
/// </summary>
public partial class LogViewModel : ObservableObject
{
    private readonly ILogBuffer _buffer;

    public ObservableCollection<LogEntry> Entries { get; } = new();

    public string LogDirectory { get; }

    public IAsyncRelayCommand CopyPathCommand { get; }

    /// <summary>
    /// 创建日志视图模型。
    /// </summary>
    /// <param name="buffer">日志缓冲区。</param>
    /// <param name="pathService">路径服务。</param>
    /// <param name="clipboardService">剪贴板服务。</param>
    public LogViewModel(ILogBuffer buffer, IAppPathService pathService, IClipboardService clipboardService)
    {
        _buffer = buffer;
        LogDirectory = pathService.LogDirectory;
        CopyPathCommand = new AsyncRelayCommand(() => clipboardService.SetTextAsync(LogDirectory));

        // 先加载已有日志
        foreach (var entry in buffer.GetSnapshot())
        {
            Entries.Add(entry);
        }

        // 订阅新增日志
        buffer.LogAdded += OnLogAdded;
    }

    /// <summary>
    /// 处理新增日志事件。
    /// </summary>
    /// <param name="entry">日志条目。</param>
    private void OnLogAdded(LogEntry entry)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Entries.Add(entry);
            while (Entries.Count > _buffer.Capacity)
            {
                Entries.RemoveAt(0);
            }
        });
    }
}
