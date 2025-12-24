using System;
using Avalonia.Controls;
using AutoSerialPort.Application;
using AutoSerialPort.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoSerialPort.UI.Views;

/// <summary>
/// 主窗口视图。
/// </summary>
public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow>? _logger;
    private bool _isInitialized = false;

    /// <summary>
    /// 创建主窗口并绑定视图模型。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        if (Design.IsDesignMode)
        {
            return;
        }

        try
        {
            _logger = AppServices.Provider?.GetService<ILogger<MainWindow>>();
            DataContext = AppServices.GetRequired<MainWindowViewModel>();

            // 首次打开时初始化配置
            Opened += OnWindowOpened;

            // 关闭时隐藏窗口，保持托盘运行
            Closing += (_, e) =>
            {
                e.Cancel = true;
                Hide();
                _logger?.LogDebug("主窗口已隐藏");
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "初始化主窗口时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 窗口打开事件处理（简化版本）
    /// </summary>
    private async void OnWindowOpened(object? sender, EventArgs e)
    {
        // 防止重复初始化
        if (_isInitialized)
        {
            _logger?.LogDebug("主窗口已初始化，跳过重复初始化");
            return;
        }

        try
        {
            _logger?.LogInformation("主窗口打开，开始初始化");

            if (DataContext is MainWindowViewModel vm)
            {
                await vm.InitializeAsync();
                _isInitialized = true;
                _logger?.LogInformation("主窗口初始化完成");
            }
            else
            {
                _logger?.LogWarning("MainWindowViewModel未找到，无法初始化");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "主窗口初始化时发生错误");
        }
    }
}
