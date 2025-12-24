using AutoSerialPort.Domain.Models;

namespace AutoSerialPort.Application.Abstractions;

/// <summary>
/// 设备配置视图模型接口
/// 提供设备配置的抽象表示，避免Application层直接依赖UI层
/// </summary>
public interface IDeviceProfileViewModel
{
    /// <summary>
    /// 设备ID
    /// </summary>
    long DeviceId { get; }

    /// <summary>
    /// 显示名称
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// 是否已启用
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// 是否正在运行
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// 串口设置（UI层特有属性，Application层实现可返回null）
    /// </summary>
    ISerialSettingsViewModel? SerialSettings { get; }

    /// <summary>
    /// 解析器设置（UI层特有属性，Application层实现可返回null）
    /// </summary>
    IParserSettingsViewModel? ParserSettings { get; }

    /// <summary>
    /// 转发器设置（UI层特有属性，Application层实现可返回null）
    /// </summary>
    IForwarderSettingsViewModel? ForwarderSettings { get; }

    /// <summary>
    /// 加载设备配置
    /// </summary>
    /// <param name="profile">设备配置</param>
    void Load(SerialDeviceProfile profile);

    /// <summary>
    /// 构建设备配置
    /// </summary>
    /// <returns>设备配置</returns>
    SerialDeviceProfile BuildProfile();

    /// <summary>
    /// 更新运行状态
    /// </summary>
    /// <param name="status">设备状态</param>
    void UpdateStatus(SerialDeviceStatus? status);
}