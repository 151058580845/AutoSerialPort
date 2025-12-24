using System;
using System.IO.Ports;
using System.Net.Sockets;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Entities;
using AutoSerialPort.Domain.Models;
using AutoSerialPort.Infrastructure.Factories;
using AutoSerialPort.Infrastructure.Forwarders;
using Serilog;

namespace AutoSerialPort.Infrastructure.Serial;

/// <summary>
/// 单设备串口运行器，负责连接、读取、解析与转发。
/// </summary>
public class SerialDeviceRunner
{
    private readonly ParserFactory _parserFactory;
    private readonly FrameDecoderFactory _frameDecoderFactory;
    private readonly ForwarderFactory _forwarderFactory;
    private readonly ISerialPortLocator _portLocator;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly object _portSync = new();
    private readonly object _runSync = new();
    private SerialPort? _serialPort;

    private SerialDeviceProfile _profile;
    private IParser _parser;
    private IFrameDecoder _frameDecoder;
    private IReadOnlyList<IForwarder> _forwarders;
    private string? _forwarderError;
    private string? _connectionError;

    private CancellationTokenSource? _runCts;
    private Task? _runTask;
    private bool _manualOverride;
    private bool _manualStop;

    private readonly SerialDeviceStatus _status = new();
    private long _totalMessages;
    private long _lastTotal;
    private DateTime _lastRateTime = DateTime.UtcNow;

    /// <summary>
    /// 原始字节流事件。
    /// </summary>
    public event EventHandler<SerialRawDataEventArgs>? RawDataReceived;

    public SerialDeviceRunner(
        SerialDeviceProfile profile,
        ParserFactory parserFactory,
        FrameDecoderFactory frameDecoderFactory,
        ForwarderFactory forwarderFactory,
        ISerialPortLocator portLocator)
    {
        _profile = profile;
        _parserFactory = parserFactory;
        _frameDecoderFactory = frameDecoderFactory;
        _forwarderFactory = forwarderFactory;
        _portLocator = portLocator;
        _parser = _parserFactory.Create(profile.Parser);
        _frameDecoder = _frameDecoderFactory.Create(profile.FrameDecoder);
        _forwarders = _forwarderFactory.Create(profile.Forwarders);
        UpdateStatus(SerialConnectionState.Disconnected, null, null);
    }

    /// <summary>
    /// 设备 Id。
    /// </summary>
    public long DeviceId => _profile.Serial.Id;

    /// <summary>
    /// 是否已启动。
    /// </summary>
    public bool IsRunning => _runTask != null;

    /// <summary>
    /// 获取当前设备状态。
    /// </summary>
    public SerialDeviceStatus GetStatus()
    {
        UpdateRate();
        return _status;
    }

    /// <summary>
    /// 发送数据到当前串口。
    /// </summary>
    /// <param name="data">待发送数据。</param>
    /// <param name="ct">取消令牌。</param>
    public async Task<SerialSendResult> SendAsync(byte[] data, CancellationToken ct)
    {
        if (data.Length == 0)
        {
            return SerialSendResult.Fail("发送内容为空");
        }

        SerialPort? port;
        lock (_portSync)
        {
            port = _serialPort;
        }

        if (port == null || !port.IsOpen || _status.ConnectionState != SerialConnectionState.Connected)
        {
            return SerialSendResult.Fail("设备未连接");
        }

        await _writeLock.WaitAsync(ct);
        try
        {
            lock (_portSync)
            {
                port = _serialPort;
            }

            if (port == null || !port.IsOpen)
            {
                return SerialSendResult.Fail("设备未连接");
            }

            await port.BaseStream.WriteAsync(data.AsMemory(0, data.Length), ct);
            return SerialSendResult.Ok();
        }
        catch (Exception ex)
        {
            return SerialSendResult.Fail(ex.Message);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// 启动设备采集。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    public async Task StartAsync(CancellationToken ct)
    {
        await StartInternalAsync(ct, manualStart: false);
    }

    /// <summary>
    /// 停止设备采集。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    public async Task StopAsync(CancellationToken ct)
    {
        if (_runCts == null || _runTask == null)
        {
            return;
        }

        await _runCts.CancelAsync();
        await _runTask;
        _runCts.Dispose();
        _runCts = null;
        _runTask = null;

        await StopForwardersAsync(ct);
        UpdateStatus(SerialConnectionState.Disconnected, null, null);
        lock (_runSync)
        {
            _manualOverride = false;
        }
    }

    /// <summary>
    /// 手动启动设备采集。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    public async Task StartManualAsync(CancellationToken ct)
    {
        await StartInternalAsync(ct, manualStart: true);
    }

    /// <summary>
    /// 手动停止设备采集。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    public async Task StopManualAsync(CancellationToken ct)
    {
        lock (_runSync)
        {
            _manualStop = true;
            _manualOverride = false;
        }

        await StopAsync(ct);
    }

    /// <summary>
    /// 应用新的设备配置，必要时触发重启。
    /// </summary>
    /// <param name="profile">新的设备配置。</param>
    /// <param name="ct">取消令牌。</param>
    public async Task ApplyProfileAsync(SerialDeviceProfile profile, CancellationToken ct)
    {
        var restartNeeded = !SerialConfigEquals(_profile.Serial, profile.Serial)
            || !string.Equals(_profile.Serial.IdentifierType, profile.Serial.IdentifierType, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(_profile.Serial.IdentifierValue, profile.Serial.IdentifierValue, StringComparison.OrdinalIgnoreCase)
            || _profile.Serial.IsEnabled != profile.Serial.IsEnabled;

        var autoStartEnabled = _profile.Serial.IsEnabled;
        _profile = profile;
        if (!autoStartEnabled && _profile.Serial.IsEnabled)
        {
            lock (_runSync)
            {
                _manualStop = false;
            }
        }

        _parser = _parserFactory.Create(profile.Parser);
        _frameDecoder = _frameDecoderFactory.Create(profile.FrameDecoder);

        // 转发器配置变更需要重新创建
        await StopForwardersAsync(ct);
        _forwarders = _forwarderFactory.Create(profile.Forwarders);
        await StartForwardersAsync(ct);

        // 串口参数变化时执行重启
        if (restartNeeded && _runTask != null)
        {
            await StopAsync(ct);
            if (ShouldAutoStart())
            {
                await StartAsync(ct);
            }
            else if (ShouldManualOverride())
            {
                await StartManualAsync(ct);
            }
        }
    }

    /// <summary>
    /// 串口主循环，包含连接、读取与解析管线。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    private async Task RunAsync(CancellationToken ct)
    {
        var retryDelay = new RetryDelay(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
        while (!ct.IsCancellationRequested)
        {
            if (!_profile.Serial.IsEnabled)
            {
                if (!ShouldManualOverride())
                {
                    UpdateStatus(SerialConnectionState.Disconnected, null, null);
                    await Task.Delay(TimeSpan.FromSeconds(1), ct);
                    continue;
                }

                UpdateStatus(SerialConnectionState.Connecting, null, null);
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }

            try
            {
                UpdateStatus(SerialConnectionState.Connecting, null, null);
                var portName = await _portLocator.ResolvePortNameAsync(_profile.Serial, ct);
                if (string.IsNullOrWhiteSpace(portName))
                {
                    UpdateStatus(SerialConnectionState.Reconnecting, "Port not found", null);
                    await Task.Delay(retryDelay.Next(), ct);
                    continue;
                }

                // 建立串口连接
                using var serialPort = CreateSerialPort(_profile.Serial, portName);
                serialPort.Open();
                _frameDecoder.Reset();
                SetActivePort(serialPort);
                UpdateStatus(SerialConnectionState.Connected, null, portName);
                retryDelay.Reset();

                // 使用 Channel 解耦读取与解析
                var channel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = true
                });

                try
                {
                    var readTask = Task.Run(() => ReadLoopAsync(serialPort, channel.Writer, ct), ct);
                    var parseTask = Task.Run(() => ParseLoopAsync(channel.Reader, ct), ct);

                    await Task.WhenAll(readTask, parseTask);
                }
                finally
                {
                    ClearActivePort(serialPort);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                UpdateStatus(SerialConnectionState.Reconnecting, ex.Message, _status.PortName);
                Log.Error(ex, "Serial device {Device} failed, retrying", _profile.Serial.DisplayName);
                await Task.Delay(retryDelay.Next(), ct);
            }
        }
    }

    /// <summary>
    /// 读取串口数据并写入通道。
    /// </summary>
    /// <param name="serialPort">已打开的串口。</param>
    /// <param name="writer">通道写入端。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task ReadLoopAsync(SerialPort serialPort, ChannelWriter<byte[]> writer, CancellationToken ct)
    {
        var buffer = new byte[4096];
        const int stableWaitMs = 10; // 等待数据稳定的时间
        const int maxWaitMs = 100;   // 最大等待时间，避免无限等待
        
        while (!ct.IsCancellationRequested && serialPort.IsOpen)
        {
            try
            {
                // 等待有数据到达
                while (serialPort.BytesToRead == 0 && !ct.IsCancellationRequested && serialPort.IsOpen)
                {
                    await Task.Delay(1, ct);
                }
    
                if (ct.IsCancellationRequested || !serialPort.IsOpen)
                    break;
    
                // 智能等待策略：等待数据稳定
                var lastBytesToRead = serialPort.BytesToRead;
                var waitTime = 0;
                
                while (waitTime < maxWaitMs && !ct.IsCancellationRequested && serialPort.IsOpen)
                {
                    await Task.Delay(stableWaitMs, ct);
                    waitTime += stableWaitMs;
                    
                    var currentBytesToRead = serialPort.BytesToRead;
                    
                    // 如果数据量稳定（连续两次检查数据量相同），则认为一个完整包已到达
                    if (currentBytesToRead > 0 && currentBytesToRead == lastBytesToRead)
                    {
                        break;
                    }
                    
                    lastBytesToRead = currentBytesToRead;
                }
    
                if (ct.IsCancellationRequested || !serialPort.IsOpen)
                    break;
    
                // 读取所有可用数据
                var availableBytes = serialPort.BytesToRead;
                if (availableBytes > 0)
                {
                    // 确保缓冲区足够大
                    if (availableBytes > buffer.Length)
                    {
                        buffer = new byte[Math.Max(availableBytes, buffer.Length * 2)];
                    }
    
                    var bytesRead = await serialPort.BaseStream.ReadAsync(buffer.AsMemory(0, availableBytes), ct);
                    if (bytesRead > 0)
                    {
                        // 拷贝有效数据，避免复用缓冲区导致的数据污染
                        var copy = new byte[bytesRead];
                        Buffer.BlockCopy(buffer, 0, copy, 0, bytesRead);
                        
                        try
                        {
                            RawDataReceived?.Invoke(this, new SerialRawDataEventArgs(DeviceId, copy, DateTimeOffset.Now));
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Serial device {Device} raw data handler failed", _profile.Serial.DisplayName);
                        }
                        
                        await writer.WriteAsync(copy, ct);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Serial device {Device} read loop error", _profile.Serial.DisplayName);
                // 短暂延迟后继续，避免错误循环
                await Task.Delay(100, ct);
            }
        }
    
        writer.TryComplete();
    }

    /// <summary>
    /// 解析通道中的数据并转发。
    /// </summary>
    /// <param name="reader">通道读取端。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task ParseLoopAsync(ChannelReader<byte[]> reader, CancellationToken ct)
    {
        await foreach (var data in reader.ReadAllAsync(ct))
        {
            var decoder = _frameDecoder;
            // 先按拆包规则切分完整帧，避免粘包/半包影响解析
            var frames = decoder.Decode(data);
            if (frames.Count == 0)
            {
                continue;
            }

            var parser = _parser;
            foreach (var frame in frames)
            {
                var messages = await parser.ParseAsync(frame, frame.Length, ct);
                if (messages.Count == 0)
                {
                    continue;
                }

                foreach (var message in messages)
                {
                    // 统计吞吐量
                    Interlocked.Increment(ref _totalMessages);
                    await ForwardToAllAsync(message, ct);
                }
            }
        }
    }

    /// <summary>
    /// 将消息转发到所有启用的转发器。
    /// </summary>
    /// <param name="message">解析后的消息。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task ForwardToAllAsync(ParsedMessage message, CancellationToken ct)
    {
        var forwarders = _forwarders;
        foreach (var forwarder in forwarders)
        {
            if (!forwarder.IsEnabled)
            {
                continue;
            }

            try
            {
                await forwarder.ForwardAsync(message, ct);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Forwarder {Forwarder} failed", forwarder.Name);
            }
        }
    }

    /// <summary>
    /// 启动全部转发器。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    private async Task StartForwardersAsync(CancellationToken ct)
    {
        string? firstError = null;
        foreach (var forwarder in _forwarders)
        {
            try
            {
                await forwarder.StartAsync(ct);
            }
            catch (Exception ex)
            {
                firstError ??= BuildForwarderError(forwarder, ex);
                Log.Error(ex, "Forwarder {Forwarder} start failed", forwarder.Name);
            }
        }

        _forwarderError = firstError;
        RefreshLastError();
    }

    /// <summary>
    /// 停止全部转发器。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    private async Task StopForwardersAsync(CancellationToken ct)
    {
        foreach (var forwarder in _forwarders)
        {
            try
            {
                await forwarder.StopAsync(ct);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Forwarder {Forwarder} stop failed", forwarder.Name);
            }
        }
    }

    /// <summary>
    /// 创建串口实例并应用端口参数。
    /// </summary>
    /// <param name="config">设备配置。</param>
    /// <param name="portName">实际串口名。</param>
    private static SerialPort CreateSerialPort(SerialDeviceConfig config, string portName)
    {
        var port = new SerialPort
        {
            PortName = portName,
            BaudRate = config.BaudRate,
            DataBits = config.DataBits,
            Parity = ParseParity(config.Parity),
            StopBits = ParseStopBits(config.StopBits)
        };

        return port;
    }

    /// <summary>
    /// 解析校验位配置。
    /// </summary>
    /// <param name="parity">校验位字符串。</param>
    private static Parity ParseParity(string? parity)
    {
        if (Enum.TryParse<Parity>(parity, true, out var value))
        {
            return value;
        }

        return Parity.None;
    }

    /// <summary>
    /// 解析停止位配置。
    /// </summary>
    /// <param name="stopBits">停止位字符串。</param>
    private static StopBits ParseStopBits(string? stopBits)
    {
        if (Enum.TryParse<StopBits>(stopBits, true, out var value))
        {
            return value;
        }

        return StopBits.One;
    }

    /// <summary>
    /// 判断串口参数是否一致。
    /// </summary>
    private static bool SerialConfigEquals(SerialDeviceConfig left, SerialDeviceConfig right)
    {
        return left.BaudRate == right.BaudRate
            && string.Equals(left.Parity, right.Parity, StringComparison.OrdinalIgnoreCase)
            && left.DataBits == right.DataBits
            && string.Equals(left.StopBits, right.StopBits, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 更新运行状态。
    /// </summary>
    /// <param name="state">连接状态。</param>
    /// <param name="error">错误信息。</param>
    /// <param name="portName">串口名。</param>
    private void UpdateStatus(SerialConnectionState state, string? error, string? portName)
    {
        _status.DeviceId = _profile.Serial.Id;
        _status.DisplayName = _profile.Serial.DisplayName;
        _status.PortName = portName;
        _status.ConnectionState = state;
        _status.IsRunning = _runTask != null;
        _connectionError = error;
        RefreshLastError();
        _status.ParserName = _parser.Name;
        _status.ActiveForwarders = _forwarders.Where(x => x.IsEnabled).Select(x => x.Name).ToArray();
    }

    private void RefreshLastError()
    {
        _status.LastError = !string.IsNullOrWhiteSpace(_connectionError)
            ? _connectionError
            : _forwarderError;
    }

    private static string BuildForwarderError(IForwarder forwarder, Exception ex)
    {
        var root = ex;
        if (ex is AggregateException aggregate)
        {
            root = aggregate.Flatten().InnerExceptions.FirstOrDefault() ?? ex;
        }

        string reason;
        if (root is SocketException socketEx && socketEx.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            reason = "端口已被占用";
        }
        else
        {
            reason = root.Message;
        }

        return $"{forwarder.Name} 启动失败: {reason}";
    }

    /// <summary>
    /// 计算吞吐量。
    /// </summary>
    private void UpdateRate()
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastRateTime).TotalSeconds;
        if (elapsed <= 0.1)
        {
            return;
        }

        var total = Interlocked.Read(ref _totalMessages);
        var delta = total - _lastTotal;
        _status.TotalMessages = total;
        _status.MessagesPerSecond = delta / elapsed;
        _lastTotal = total;
        _lastRateTime = now;
    }

    private void SetActivePort(SerialPort port)
    {
        lock (_portSync)
        {
            _serialPort = port;
        }
    }

    private void ClearActivePort(SerialPort port)
    {
        lock (_portSync)
        {
            if (ReferenceEquals(_serialPort, port))
            {
                _serialPort = null;
            }
        }
    }

    private async Task StartInternalAsync(CancellationToken ct, bool manualStart)
    {
        Task? runTask;
        lock (_runSync)
        {
            if (manualStart)
            {
                _manualStop = false;
                _manualOverride = true;
            }

            if (_runTask != null)
            {
                _status.IsRunning = true;
                return;
            }

            if (!manualStart && !ShouldAutoStart())
            {
                _status.IsRunning = false;
                return;
            }

            // 统一使用联动取消令牌，避免资源泄漏
            _runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _runTask = Task.Run(() => RunAsync(_runCts.Token));
            _status.IsRunning = true;
            runTask = _runTask;
        }

        if (runTask != null)
        {
            await StartForwardersAsync(ct);
        }
    }

    private bool ShouldAutoStart()
    {
        lock (_runSync)
        {
            return _profile.Serial.IsEnabled && !_manualStop;
        }
    }

    private bool ShouldManualOverride()
    {
        lock (_runSync)
        {
            return _manualOverride;
        }
    }
}
