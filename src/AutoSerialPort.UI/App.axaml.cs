using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using AutoSerialPort.Application;
using AutoSerialPort.UI.ViewModels;
using Avalonia.Threading;

namespace AutoSerialPort.UI;

/// <summary>
/// Avalonia 应用入口。
/// </summary>
public partial class App : Avalonia.Application
{
    /// <summary>
    /// 初始化应用资源。
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        RequestedThemeVariant = ThemeVariant.Light;
    }

    /// <summary>
    /// 框架初始化完成后配置托盘与主窗口行为。
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
        }

        if (Design.IsDesignMode)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }
        // 初始化托盘菜单
        var trayIcons = TrayIcon.GetIcons(this);
        var tray = trayIcons?.FirstOrDefault();
        if (tray != null)
        {
            var trayVm = AppServices.GetRequired<TrayViewModel>();
            tray.Clicked += (_, _) => trayVm.OpenMainWindowCommand.Execute(null);
            var autoStartItem = new NativeMenuItem
            {
                Header = "自动启动",
                ToggleType = NativeMenuItemToggleType.CheckBox
            };
            autoStartItem.Click += async (_, _) =>
            {
                trayVm.IsAutoStartEnabled = !trayVm.IsAutoStartEnabled;
                await trayVm.ToggleAutoStartCommand.ExecuteAsync(null);
                autoStartItem.IsChecked = trayVm.IsAutoStartEnabled;
            };

            var openItem = new NativeMenuItem { Header = "主界面" };
            openItem.Click += (_, _) => trayVm.OpenMainWindowCommand.Execute(null);

            var exitItem = new NativeMenuItem { Header = "退出" };
            exitItem.Click += (_, _) => trayVm.ExitCommand.Execute(null);

            tray.Menu = new NativeMenu
            {
                Items =
                {
                    autoStartItem,
                    new NativeMenuItemSeparator(),
                    openItem,
                    exitItem
                }
            };

            _ = InitializeTrayAsync(trayVm, autoStartItem);
            trayVm.OpenMainWindowCommand.Execute(null);
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// 初始化托盘菜单绑定状态。
    /// </summary>
    /// <param name="trayVm">托盘视图模型。</param>
    /// <param name="autoStartItem">自动启动菜单项。</param>
    private static async Task InitializeTrayAsync(TrayViewModel trayVm, NativeMenuItem autoStartItem)
    {
        await trayVm.InitializeAsync();
        Dispatcher.UIThread.Post(() => autoStartItem.IsChecked = trayVm.IsAutoStartEnabled);

        // 同步自动启动状态到菜单勾选
        trayVm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(TrayViewModel.IsAutoStartEnabled))
            {
                Dispatcher.UIThread.Post(() => autoStartItem.IsChecked = trayVm.IsAutoStartEnabled);
            }
        };
    }
}
