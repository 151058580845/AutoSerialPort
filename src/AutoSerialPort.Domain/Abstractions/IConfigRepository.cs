using System.Threading.Tasks;
using AutoSerialPort.Domain.Entities;
using AutoSerialPort.Domain.Models;

namespace AutoSerialPort.Domain.Abstractions;

/// <summary>
/// 配置仓储接口，负责配置的读取与持久化。
/// </summary>
public interface IConfigRepository
{
    /// <summary>
    /// 获取全局应用设置。
    /// </summary>
    Task<AppSettings> GetAppSettingsAsync();

    /// <summary>
    /// 保存全局应用设置。
    /// </summary>
    /// <param name="settings">应用设置。</param>
    Task SaveAppSettingsAsync(AppSettings settings);

    /// <summary>
    /// 获取指定方案的串口设备配置集合。
    /// </summary>
    /// <param name="profileId">方案 Id。</param>
    Task<SerialDeviceProfile[]> GetSerialDeviceProfilesAsync(long profileId);

    /// <summary>
    /// 保存指定方案的串口设备配置集合。
    /// </summary>
    /// <param name="profileId">方案 Id。</param>
    /// <param name="profiles">设备配置集合。</param>
    Task SaveSerialDeviceProfilesAsync(long profileId, SerialDeviceProfile[] profiles);

    /// <summary>
    /// 获取全部方案列表。
    /// </summary>
    Task<Profile[]> GetProfilesAsync();

    /// <summary>
    /// 确保存在默认方案，必要时自动创建并返回 Id。
    /// </summary>
    Task<long> EnsureDefaultProfileAsync();
}
