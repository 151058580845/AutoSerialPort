using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoSerialPort.Application.Models;
using AutoSerialPort.Domain.Entities;
using AutoSerialPort.Domain.Models;

namespace AutoSerialPort.Application.Abstractions;

/// <summary>
/// 串口设置视图模型接口
/// 提供串口配置的抽象表示
/// </summary>
public interface ISerialSettingsViewModel
{
    /// <summary>
    /// 串口配置
    /// </summary>
    SerialDeviceConfig Config { get; }

    /// <summary>
    /// 可用端口列表
    /// </summary>
    ObservableCollection<SerialPortDescriptor> Ports { get; }

    /// <summary>
    /// 当前选中的端口
    /// </summary>
    SerialPortDescriptor? SelectedPort { get; set; }

    /// <summary>
    /// 刷新端口命令
    /// </summary>
    ICommand RefreshPortsCommand { get; }

    /// <summary>
    /// 校验选项
    /// </summary>
    ObservableCollection<OptionItem> ParityOptions { get; }

    /// <summary>
    /// 选中的校验选项
    /// </summary>
    OptionItem? SelectedParityOption { get; set; }

    /// <summary>
    /// 停止位选项
    /// </summary>
    ObservableCollection<OptionItem> StopBitsOptions { get; }

    /// <summary>
    /// 选中的停止位选项
    /// </summary>
    OptionItem? SelectedStopBitsOption { get; set; }

    /// <summary>
    /// 识别类型选项
    /// </summary>
    ObservableCollection<OptionItem> IdentifierTypeOptions { get; }

    /// <summary>
    /// 选中的识别类型选项
    /// </summary>
    OptionItem? SelectedIdentifierTypeOption { get; set; }

    /// <summary>
    /// 是否显示高级设置
    /// </summary>
    bool IsShowAdvancedSettings { get; set; }

    /// <summary>
    /// 显示高级设置命令
    /// </summary>
    ICommand ShowAdvancedSettingsCommand { get; }

    /// <summary>
    /// 初始化异步
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// 加载配置
    /// </summary>
    /// <param name="config">串口配置</param>
    void Load(SerialDeviceConfig config);
}