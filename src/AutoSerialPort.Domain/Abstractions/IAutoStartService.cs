using System.Threading.Tasks;

namespace AutoSerialPort.Domain.Abstractions;

/// <summary>
/// 自动启动服务，负责读取与写入系统自启动配置。
/// </summary>
public interface IAutoStartService
{
    /// <summary>
    /// 获取是否启用自动启动。
    /// </summary>
    Task<bool> GetAutoStartEnabledAsync();

    /// <summary>
    /// 设置是否启用自动启动。
    /// </summary>
    /// <param name="enabled">是否启用。</param>
    Task SetAutoStartEnabledAsync(bool enabled);
}
