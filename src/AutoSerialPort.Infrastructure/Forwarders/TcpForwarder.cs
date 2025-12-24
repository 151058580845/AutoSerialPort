using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoSerialPort.Domain.Models;
using AutoSerialPort.Domain.Options;

namespace AutoSerialPort.Infrastructure.Forwarders;

/// <summary>
/// TCP 转发器，支持客户端和服务端模式。
/// </summary>
public class TcpForwarder : ForwarderBase
{
    private readonly TcpForwarderOptions _options;
    private readonly List<TcpClient> _clients = new();
    private readonly RetryDelay _retryDelay = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
    private TcpListener? _listener;
    private TcpClient? _client;
    private CancellationTokenSource? _acceptCts;
    private Task? _acceptTask;
    private readonly object _sync = new();

    /// <summary>
    /// 创建 TCP 转发器。
    /// </summary>
    /// <param name="options">转发配置。</param>
    /// <param name="enabled">是否启用。</param>
    public TcpForwarder(TcpForwarderOptions options, bool enabled)
    {
        _options = options;
        IsEnabled = enabled;
    }

    /// <summary>
    /// 转发器名称。
    /// </summary>
    public override string Name => "TCP 转发";

    /// <summary>
    /// 启动 TCP 转发器。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    public override Task StartAsync(CancellationToken ct)
    {
        if (!IsEnabled)
        {
            return Task.CompletedTask;
        }

        if (IsServerMode())
        {
            // 服务端模式：启动监听并接受客户端
            var address = ResolveAddress(_options.Host);
            _listener = new TcpListener(address, _options.Port);
            try
            {
                _listener.Start();
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                throw new InvalidOperationException($"TCP 端口已被占用: {_options.Host}:{_options.Port}", ex);
            }
            catch (SocketException ex)
            {
                throw new InvalidOperationException($"TCP 监听启动失败: {_options.Host}:{_options.Port}", ex);
            }
            _acceptCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _acceptTask = Task.Run(() => AcceptLoopAsync(_acceptCts.Token));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 停止 TCP 转发器。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    public override async Task StopAsync(CancellationToken ct)
    {
        _acceptCts?.Cancel();
        if (_acceptTask != null)
        {
            await _acceptTask;
        }

        lock (_sync)
        {
            foreach (var client in _clients)
            {
                client.Close();
            }
            _clients.Clear();
        }

        _listener?.Stop();
        _listener = null;

        if (_client != null)
        {
            _client.Close();
            _client = null;
        }
    }

    /// <summary>
    /// 转发消息到 TCP 连接。
    /// </summary>
    /// <param name="message">解析消息。</param>
    /// <param name="ct">取消令牌。</param>
    public override async Task ForwardAsync(ParsedMessage message, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            return;
        }
        try
        {
            var payload = Encoding.UTF8.GetBytes(message.Text + "\n");
            if (IsServerMode())
            {
                // 服务端模式：广播给所有已连接客户端
                List<TcpClient> clientsCopy;
                lock (_sync)
                {
                    clientsCopy = _clients.ToList();
                }

                foreach (var client in clientsCopy)
                {
                    if (!client.Connected)
                    {
                        RemoveClient(client);
                        continue;
                    }

                    await client.GetStream().WriteAsync(payload, ct);
                }
            }
            else
            {
                // 客户端模式：确保已连接后再发送
                await EnsureClientConnectedAsync(ct);
                if (_client?.Connected == true)
                {
                    await _client.GetStream().WriteAsync(payload, ct);
                }
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "TCP forwarder failed to send data");
        }
    }

    /// <summary>
    /// 确保客户端连接可用。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    private async Task EnsureClientConnectedAsync(CancellationToken ct)
    {
        if (_client?.Connected == true)
        {
            return;
        }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                // 每次重连前先清理旧连接
                _client?.Close();
                _client = new TcpClient();
                await _client.ConnectAsync(_options.Host, _options.Port, ct);
                _retryDelay.Reset();
                return;
            }
            catch (Exception ex)
            {
                LogError(ex, "TCP client connect failed");
                await Task.Delay(_retryDelay.Next(), ct);
            }
        }
    }

    /// <summary>
    /// 服务端接受客户端连接循环。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        if (_listener == null)
        {
            return;
        }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(ct);
                client.NoDelay = true;
                lock (_sync)
                {
                    _clients.Add(client);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                LogError(ex, "TCP server accept failed");
                await Task.Delay(_retryDelay.Next(), ct);
            }
        }
    }

    /// <summary>
    /// 判断是否为服务端模式。
    /// </summary>
    private bool IsServerMode()
        => string.Equals(_options.Mode, "Server", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 将主机字符串解析为 IP 地址。
    /// </summary>
    /// <param name="host">主机字符串。</param>
    private static IPAddress ResolveAddress(string host)
    {
        if (string.IsNullOrWhiteSpace(host) || host == "0.0.0.0")
        {
            return IPAddress.Any;
        }

        if (IPAddress.TryParse(host, out var address))
        {
            return address;
        }

        return IPAddress.Any;
    }

    /// <summary>
    /// 移除并关闭客户端连接。
    /// </summary>
    /// <param name="client">客户端连接。</param>
    private void RemoveClient(TcpClient client)
    {
        lock (_sync)
        {
            _clients.Remove(client);
        }

        client.Close();
    }
}
