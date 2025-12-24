using AutoSerialPort.Domain.Entities;
using AutoSerialPort.Domain.Models;

namespace AutoSerialPort.Application.Models;

/// <summary>
/// 应用程序状态模型
/// 包含应用程序的完整状态信息，用于UI和业务逻辑之间的数据传递
/// </summary>
public class AppState
{
    /// <summary>
    /// 应用程序全局设置
    /// 包含主题、自动启动、窗口状态等配置信息
    /// </summary>
    public AppSettings Settings { get; set; } = new();

    /// <summary>
    /// 串口设备配置列表
    /// 包含当前配置文件下的所有设备配置信息
    /// </summary>
    public SerialDeviceProfile[] Devices { get; set; } = Array.Empty<SerialDeviceProfile>();

    /// <summary>
    /// 配置文件列表
    /// 包含所有可用的配置方案
    /// </summary>
    public Profile[] Profiles { get; set; } = Array.Empty<Profile>();
}
