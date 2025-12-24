using System.Threading;
using System.Threading.Tasks;
using AutoSerialPort.Domain.Entities;

namespace AutoSerialPort.Domain.Abstractions;

/// <summary>
/// 串口定位服务，负责根据设备标识定位真实串口名。
/// </summary>
public interface ISerialPortLocator
{
    /// <summary>
    /// 根据设备配置解析当前可用串口名称。
    /// </summary>
    /// <param name="config">设备配置。</param>
    /// <param name="ct">取消令牌。</param>
    Task<string?> ResolvePortNameAsync(SerialDeviceConfig config, CancellationToken ct);
}
