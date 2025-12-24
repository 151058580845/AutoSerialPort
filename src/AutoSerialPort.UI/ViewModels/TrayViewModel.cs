using System;
using System.Threading.Tasks;
using AutoSerialPort.Application.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;

namespace AutoSerialPort.UI.ViewModels;

/// <summary>
/// 托盘菜单视图模型，提供自动启动与主界面入口。
/// </summary>
public partial class TrayViewModel : ObservableObject
{
    private readonly IAppController _appController;
    private readonly Func<Views.MainWindow> _windowFactory;
    private Views.MainWindow? _window;

    [ObservableProperty]
    private bool _isAutoStartEnabled;

    public IAsyncRelayCommand ToggleAutoStartCommand { get; }
    public IRelayCommand OpenMainWindowCommand { get; }
    public IRelayCommand ExitCommand { get; }

    /// <summary>
    /// 创建托盘视图模型。
    /// </summary>
    /// <param name="appController">应用控制器。</param>
    /// <param name="windowFactory">主窗口工厂。</param>
    public TrayViewModel(IAppController appController, Func<Views.MainWindow> windowFactory)
    {
        _appController = appController;
        _windowFactory = windowFactory;
        ToggleAutoStartCommand = new AsyncRelayCommand(ToggleAutoStartAsync);
        OpenMainWindowCommand = new RelayCommand(OpenMainWindow);
        ExitCommand = new RelayCommand(Exit);
    }

    /// <summary>
    /// 初始化自动启动状态。
    /// </summary>
    public async Task InitializeAsync()
    {
        IsAutoStartEnabled = await _appController.GetAutoStartAsync();
    }

    /// <summary>
    /// 切换自动启动开关。
    /// </summary>
    private async Task ToggleAutoStartAsync()
    {
        await _appController.SetAutoStartAsync(IsAutoStartEnabled);
    }

    /// <summary>
    /// 打开或激活主窗口。
    /// </summary>
    private void OpenMainWindow()
    {
        _window ??= _windowFactory();
        if (_window.IsVisible)
        {
            _window.Activate();
            return;
        }

        _window.Show();
        _window.Activate();
    }

    /// <summary>
    /// 退出应用。
    /// </summary>
    private void Exit()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
    }
}
