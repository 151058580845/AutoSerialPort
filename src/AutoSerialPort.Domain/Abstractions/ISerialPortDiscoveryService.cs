using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoSerialPort.Domain.Models;

namespace AutoSerialPort.Domain.Abstractions;

/// <summary>
/// 串口扫描服务，负责获取系统可用串口列表。
/// </summary>
public interface ISerialPortDiscoveryService
{
    /// <summary>
    /// 获取可用串口列表。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    Task<IReadOnlyList<SerialPortDescriptor>> GetAvailablePortsAsync(CancellationToken ct);
}
