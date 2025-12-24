using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoSerialPort.Domain.Models;
using AutoSerialPort.Domain.Options;
using MQTTnet;
using MQTTnet.Client;

namespace AutoSerialPort.Infrastructure.Forwarders;

/// <summary>
/// MQTT 转发器，支持发布到指定主题。
/// </summary>
public class MqttForwarder : ForwarderBase
{
    private readonly MqttForwarderOptions _options;
    private readonly RetryDelay _retryDelay = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
    private IMqttClient? _client;
    private MqttClientOptions? _clientOptions;

    /// <summary>
    /// 创建 MQTT 转发器。
    /// </summary>
    /// <param name="options">转发配置。</param>
    /// <param name="enabled">是否启用。</param>
    public MqttForwarder(MqttForwarderOptions options, bool enabled)
    {
        _options = options;
        IsEnabled = enabled;
    }

    /// <summary>
    /// 转发器名称。
    /// </summary>
    public override string Name => "MQTT 转发";

    /// <summary>
    /// 启动 MQTT 转发器。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    public override async Task StartAsync(CancellationToken ct)
    {
        if (!IsEnabled)
        {
            return;
        }

        // 初始化客户端与连接参数
        _client ??= new MqttFactory().CreateMqttClient();
        _clientOptions = BuildOptions();

        await EnsureConnectedAsync(ct);
    }

    /// <summary>
    /// 停止 MQTT 转发器。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    public override async Task StopAsync(CancellationToken ct)
    {
        if (_client != null && _client.IsConnected)
        {
            await _client.DisconnectAsync(new MqttClientDisconnectOptions(), ct);
        }
    }

    /// <summary>
    /// 转发消息到 MQTT 主题。
    /// </summary>
    /// <param name="message">解析消息。</param>
    /// <param name="ct">取消令牌。</param>
    public override async Task ForwardAsync(ParsedMessage message, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            return;
        }

        if (_client == null || !_client.IsConnected)
        {
            await EnsureConnectedAsync(ct);
        }

        if (_client == null || !_client.IsConnected)
        {
            return;
        }

        try
        {
            // 构建发布消息并发送
            var qos = (MQTTnet.Protocol.MqttQualityOfServiceLevel)_options.QoS;
            var msg = new MqttApplicationMessageBuilder()
                .WithTopic(_options.Topic)
                .WithPayload(Encoding.UTF8.GetBytes(message.Text))
                .WithQualityOfServiceLevel(qos)
                .WithRetainFlag(_options.Retain)
                .Build();

            await _client.PublishAsync(msg, ct);
        }
        catch (Exception ex)
        {
            LogError(ex, "MQTT publish failed");
        }
    }

    /// <summary>
    /// 确保客户端已连接。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    private async Task EnsureConnectedAsync(CancellationToken ct)
    {
        if (_client == null || _clientOptions == null)
        {
            return;
        }

        while (!_client.IsConnected && !ct.IsCancellationRequested)
        {
            try
            {
                await _client.ConnectAsync(_clientOptions, ct);
                _retryDelay.Reset();
                return;
            }
            catch (Exception ex)
            {
                LogError(ex, "MQTT connect failed");
                await Task.Delay(_retryDelay.Next(), ct);
            }
        }
    }

    /// <summary>
    /// 构建 MQTT 客户端连接参数。
    /// </summary>
    private MqttClientOptions BuildOptions()
    {
        var builder = new MqttClientOptionsBuilder()
            .WithTcpServer(_options.Broker, _options.Port);

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            builder = builder.WithCredentials(_options.Username, _options.Password);
        }

        // if (_options.UseTls)
        // {
        //     // 允许不受信证书，降低配置复杂度
        //     builder = builder.WithTlsOptions(o =>
        //     {
        //         o.WithAllowUntrustedCertificates();
        //         o.WithIgnoreCertificateChainErrors();
        //         o.WithIgnoreCertificateRevocationErrors();
        //     });
        // }

        return builder.Build();
    }
}
