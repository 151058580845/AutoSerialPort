using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Models;
using AutoSerialPort.Infrastructure.Factories;

namespace AutoSerialPort.Infrastructure.Serial;

/// <summary>
/// 多串口采集编排服务，负责设备运行器的生命周期管理。
/// </summary>
public class SerialPipelineService : ISerialService
{
    private readonly ParserFactory _parserFactory;
    private readonly FrameDecoderFactory _frameDecoderFactory;
    private readonly ForwarderFactory _forwarderFactory;
    private readonly ISerialPortLocator _portLocator;
    private readonly object _sync = new();

    private readonly Dictionary<long, SerialDeviceRunner> _runners = new();
    private bool _isRunning;

    /// <summary>
    /// 原始字节流事件。
    /// </summary>
    public event EventHandler<SerialRawDataEventArgs>? RawDataReceived;

    public SerialPipelineService(
        ParserFactory parserFactory,
        FrameDecoderFactory frameDecoderFactory,
        ForwarderFactory forwarderFactory,
        ISerialPortLocator portLocator)
    {
        _parserFactory = parserFactory;
        _frameDecoderFactory = frameDecoderFactory;
        _forwarderFactory = forwarderFactory;
        _portLocator = portLocator;
    }

    /// <summary>
    /// 启动全部设备采集。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    public async Task StartAsync(CancellationToken ct)
    {
        _isRunning = true;
        // 复制当前列表，避免遍历中被修改
        List<SerialDeviceRunner> runners;
        lock (_sync)
        {
            runners = _runners.Values.ToList();
        }

        foreach (var runner in runners)
        {
            await runner.StartAsync(ct);
        }
    }

    /// <summary>
    /// 停止全部设备采集。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    public async Task StopAsync(CancellationToken ct)
    {
        _isRunning = false;
        // 复制当前列表，避免遍历中被修改
        List<SerialDeviceRunner> runners;
        lock (_sync)
        {
            runners = _runners.Values.ToList();
        }

        foreach (var runner in runners)
        {
            await runner.StopAsync(ct);
        }
    }

    /// <summary>
    /// 应用设备配置并尽量热更新运行器。
    /// </summary>
    /// <param name="profiles">设备配置集合。</param>
    /// <param name="ct">取消令牌。</param>
    public async Task ApplyConfigAsync(SerialDeviceProfile[] profiles, CancellationToken ct)
    {
        var incomingIds = profiles.Select(x => x.Serial.Id).ToHashSet();
        List<SerialDeviceRunner> toStop = new();

        lock (_sync)
        {
            // 先移除已不存在的设备
            foreach (var existingId in _runners.Keys.ToList())
            {
                if (!incomingIds.Contains(existingId))
                {
                    toStop.Add(_runners[existingId]);
                    _runners.Remove(existingId);
                }
            }
        }

        foreach (var runner in toStop)
        {
            runner.RawDataReceived -= HandleRawDataReceived;
            await runner.StopAsync(ct);
        }

        foreach (var profile in profiles)
        {
            SerialDeviceRunner? runner;
            lock (_sync)
            {
                _runners.TryGetValue(profile.Serial.Id, out runner);
                if (runner == null)
                {
                    // 新增设备时创建运行器
                    runner = new SerialDeviceRunner(profile, _parserFactory, _frameDecoderFactory, _forwarderFactory, _portLocator);
                    runner.RawDataReceived += HandleRawDataReceived;
                    _runners[profile.Serial.Id] = runner;
                }
            }

            // 应用配置，必要时重启
            await runner.ApplyProfileAsync(profile, ct);
            if (_isRunning)
            {
                await runner.StartAsync(ct);
            }
        }
    }

    /// <summary>
    /// 获取全局状态快照。
    /// </summary>
    public SerialStatusSnapshot GetStatus()
    {
        SerialDeviceStatus[] deviceStatuses;
        lock (_sync)
        {
            deviceStatuses = _runners.Values.Select(x => x.GetStatus()).ToArray();
        }

        // 聚合所有设备状态
        var snapshot = new SerialStatusSnapshot
        {
            DeviceStatuses = deviceStatuses,
            TotalMessages = deviceStatuses.Sum(x => x.TotalMessages),
            MessagesPerSecond = deviceStatuses.Sum(x => x.MessagesPerSecond)
        };

        if (deviceStatuses.Length == 0)
        {
            snapshot.ConnectionState = SerialConnectionState.Disconnected;
            snapshot.ParserName = string.Empty;
            snapshot.ActiveForwarders = Array.Empty<string>();
            return snapshot;
        }

        var runningStatuses = deviceStatuses.Where(x => x.IsRunning).ToArray();
        if (runningStatuses.Length == 0)
        {
            snapshot.ConnectionState = SerialConnectionState.Disconnected;
            snapshot.ParserName = string.Empty;
            snapshot.ActiveForwarders = Array.Empty<string>();
            return snapshot;
        }

        // 汇总全局连接状态
        snapshot.ConnectionState = runningStatuses.Any(x => x.ConnectionState == SerialConnectionState.Connected)
            ? SerialConnectionState.Connected
            : runningStatuses.Any(x => x.ConnectionState == SerialConnectionState.Connecting)
                ? SerialConnectionState.Connecting
                : SerialConnectionState.Reconnecting;

        // 单设备直接显示解析器名称，多设备显示聚合标记
        snapshot.ParserName = runningStatuses.Length == 1
            ? runningStatuses[0].ParserName
            : "Multiple";

        // 汇总启用中的转发器名称
        snapshot.ActiveForwarders = runningStatuses
            .SelectMany(x => x.ActiveForwarders.Select(f => $"{x.DisplayName}:{f}"))
            .Distinct()
            .ToArray();

        // 取第一条错误用于全局提示
        snapshot.LastError = runningStatuses.Select(x => x.LastError).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        return snapshot;
    }

    /// <summary>
    /// 发送数据到指定设备。
    /// </summary>
    /// <param name="deviceId">设备 ID。</param>
    /// <param name="data">待发送数据。</param>
    /// <param name="ct">取消令牌。</param>
    public Task<SerialSendResult> SendAsync(long deviceId, byte[] data, CancellationToken ct)
    {
        SerialDeviceRunner? runner;
        lock (_sync)
        {
            _runners.TryGetValue(deviceId, out runner);
        }

        if (runner == null)
        {
            return Task.FromResult(SerialSendResult.Fail("设备未连接"));
        }

        return runner.SendAsync(data, ct);
    }

    /// <summary>
    /// 手动启动指定设备。
    /// </summary>
    /// <param name="deviceId">设备 ID。</param>
    /// <param name="ct">取消令牌。</param>
    public async Task<SerialOperationResult> StartDeviceAsync(long deviceId, CancellationToken ct)
    {
        SerialDeviceRunner? runner;
        lock (_sync)
        {
            _runners.TryGetValue(deviceId, out runner);
        }

        if (runner == null)
        {
            return SerialOperationResult.Fail("设备不存在");
        }

        await runner.StartManualAsync(ct);
        return SerialOperationResult.Ok();
    }

    /// <summary>
    /// 手动停止指定设备。
    /// </summary>
    /// <param name="deviceId">设备 ID。</param>
    /// <param name="ct">取消令牌。</param>
    public async Task<SerialOperationResult> StopDeviceAsync(long deviceId, CancellationToken ct)
    {
        SerialDeviceRunner? runner;
        lock (_sync)
        {
            _runners.TryGetValue(deviceId, out runner);
        }

        if (runner == null)
        {
            return SerialOperationResult.Fail("设备不存在");
        }

        await runner.StopManualAsync(ct);
        return SerialOperationResult.Ok();
    }

    private void HandleRawDataReceived(object? sender, SerialRawDataEventArgs e)
    {
        RawDataReceived?.Invoke(this, e);
    }
}
