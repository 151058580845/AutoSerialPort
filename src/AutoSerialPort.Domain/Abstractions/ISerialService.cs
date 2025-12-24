using System;
using System.Threading;
using System.Threading.Tasks;
using AutoSerialPort.Domain.Models;

namespace AutoSerialPort.Domain.Abstractions;

/// <summary>
/// 串口采集服务接口，负责多设备采集的启动、停止与配置应用。
/// </summary>
public interface ISerialService
{
    /// <summary>
    /// 原始字节流接收事件。
    /// </summary>
    event EventHandler<SerialRawDataEventArgs>? RawDataReceived;

    /// <summary>
    /// 启动串口采集服务。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    Task StartAsync(CancellationToken ct);

    /// <summary>
    /// 停止串口采集服务。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    Task StopAsync(CancellationToken ct);

    /// <summary>
    /// 应用最新的串口设备配置。
    /// </summary>
    /// <param name="profiles">设备配置列表。</param>
    /// <param name="ct">取消令牌。</param>
    Task ApplyConfigAsync(SerialDeviceProfile[] profiles, CancellationToken ct);

    /// <summary>
    /// 获取当前采集状态快照。
    /// </summary>
    SerialStatusSnapshot GetStatus();

    /// <summary>
    /// 发送数据到指定设备。
    /// </summary>
    /// <param name="deviceId">设备 ID。</param>
    /// <param name="data">要发送的数据。</param>
    /// <param name="ct">取消令牌。</param>
    Task<SerialSendResult> SendAsync(long deviceId, byte[] data, CancellationToken ct);

    /// <summary>
    /// 手动启动指定设备。
    /// </summary>
    /// <param name="deviceId">设备 ID。</param>
    /// <param name="ct">取消令牌。</param>
    Task<SerialOperationResult> StartDeviceAsync(long deviceId, CancellationToken ct);

    /// <summary>
    /// 手动停止指定设备。
    /// </summary>
    /// <param name="deviceId">设备 ID。</param>
    /// <param name="ct">取消令牌。</param>
    Task<SerialOperationResult> StopDeviceAsync(long deviceId, CancellationToken ct);
}
