using System;
using System.Threading.Tasks;
using AutoSerialPort.Domain.Entities;
using SqlSugar;

namespace AutoSerialPort.Infrastructure.Persistence;

/// <summary>
/// SQLSugar 初始化器，负责建表与默认数据写入。
/// </summary>
public class SqlSugarInitializer
{
    private readonly SqlSugarScope _db;

    /// <summary>
    /// 创建初始化器。
    /// </summary>
    /// <param name="db">数据库上下文。</param>
    public SqlSugarInitializer(SqlSugarScope db)
    {
        _db = db;
    }

    /// <summary>
    /// 同步初始化数据库与默认配置。
    /// </summary>
    public void Initialize()
    {
        // 初始化表结构
        _db.CodeFirst.InitTables(new[]
        {
            typeof(AppSettings),
            typeof(Profile),
            typeof(SerialPortConfig),
            typeof(SerialDeviceConfig),
            typeof(ParserConfig),
            typeof(FrameDecoderConfig),
            typeof(ForwarderConfig)
        });

        // 初始化默认方案与设置
        if (!_db.Queryable<Profile>().Any())
        {
            _db.Insertable(new Profile { Name = "Default" }).ExecuteCommand();
        }

        if (!_db.Queryable<AppSettings>().Any())
        {
            _db.Insertable(new AppSettings { AutoStart = false }).ExecuteCommand();
        }

        // 初始化默认串口配置
        if (!_db.Queryable<SerialPortConfig>().Any())
        {
            var portName = OperatingSystem.IsWindows() ? "COM3" : "/dev/ttyUSB0";
            _db.Insertable(new SerialPortConfig { PortName = portName }).ExecuteCommand();
        }

        // 初始化默认设备与子配置
        if (!_db.Queryable<SerialDeviceConfig>().Any())
        {
            var portName = OperatingSystem.IsWindows() ? "COM3" : "/dev/ttyUSB0";
            var deviceId = _db.Insertable(new SerialDeviceConfig
            {
                ProfileId = 1,
                DisplayName = "设备1",
                IdentifierType = "PortName",
                IdentifierValue = portName,
                BaudRate = 9600,
                Parity = "None",
                DataBits = 8,
                StopBits = "One",
                IsEnabled = true
            }).ExecuteReturnIdentity();

            EnsureDefaultsForDevice(deviceId, 1);
        }
        else
        {
            var devices = _db.Queryable<SerialDeviceConfig>().ToList();
            foreach (var device in devices)
            {
                EnsureDefaultsForDevice(device.Id, device.ProfileId);
            }
        }

        // 兼容旧版本配置
        MigrateLegacyConfigs();
    }

    /// <summary>
    /// 异步初始化数据库与默认配置（保留兼容）。
    /// </summary>
    public Task InitializeAsync()
    {
        Initialize();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 确保单个设备的解析/拆包/转发配置存在。
    /// </summary>
    /// <param name="deviceId">设备 Id。</param>
    /// <param name="profileId">方案 Id。</param>
    private void EnsureDefaultsForDevice(long deviceId, long profileId)
    {
        var hasParser = _db.Queryable<ParserConfig>()
            .Any(x => x.ProfileId == profileId && x.DeviceId == deviceId);
        if (!hasParser)
        {
            _db.Insertable(new ParserConfig
            {
                ProfileId = profileId,
                DeviceId = deviceId,
                ParserType = "LineParser",
                ParametersJson = "{\"encoding\":\"utf-8\",\"separator\":\"\\n\"}"
            }).ExecuteCommand();
        }

        var hasDecoder = _db.Queryable<FrameDecoderConfig>()
            .Any(x => x.ProfileId == profileId && x.DeviceId == deviceId);
        if (!hasDecoder)
        {
            _db.Insertable(new FrameDecoderConfig
            {
                ProfileId = profileId,
                DeviceId = deviceId,
                DecoderType = "DelimiterFrameDecoder",
                ParametersJson = "{\"encoding\":\"utf-8\",\"delimiter\":\"\\n\",\"includeDelimiter\":true,\"maxBufferLength\":65536}"
            }).ExecuteCommand();
        }

        var hasForwarders = _db.Queryable<ForwarderConfig>()
            .Any(x => x.ProfileId == profileId && x.DeviceId == deviceId);
        if (!hasForwarders)
        {
            // 默认 TCP 转发
            _db.Insertable(new ForwarderConfig
            {
                ProfileId = profileId,
                DeviceId = deviceId,
                ForwarderType = "TcpForwarder",
                IsEnabled = false,
                ParametersJson = "{\"mode\":\"Server\",\"host\":\"0.0.0.0\",\"port\":9000}"
            }).ExecuteCommand();

            // 默认 MQTT 转发
            _db.Insertable(new ForwarderConfig
            {
                ProfileId = profileId,
                DeviceId = deviceId,
                ForwarderType = "MqttForwarder",
                IsEnabled = false,
                ParametersJson = "{\"broker\":\"localhost\",\"port\":1883,\"topic\":\"demo/topic\",\"qos\":0,\"retain\":false}"
            }).ExecuteCommand();

            // 剪贴板转发
            _db.Insertable(new ForwarderConfig
            {
                ProfileId = profileId,
                DeviceId = deviceId,
                ForwarderType = "ClipboardForwarder",
                IsEnabled = false,
                ParametersJson = "{\"appendNewLine\":false}"
            }).ExecuteCommand();

            // 模拟输入转发
            _db.Insertable(new ForwarderConfig
            {
                ProfileId = profileId,
                DeviceId = deviceId,
                ForwarderType = "TypingForwarder",
                IsEnabled = false,
                ParametersJson = "{\"delayMs\":0}"
            }).ExecuteCommand();
        }
    }

    /// <summary>
    /// 迁移旧版本配置到按设备维度的结构。
    /// </summary>
    private void MigrateLegacyConfigs()
    {
        var hasDevice = _db.Queryable<SerialDeviceConfig>().Any();
        if (!hasDevice)
        {
            return;
        }

        var legacyParser = _db.Queryable<ParserConfig>().Any(x => x.DeviceId == 0);
        var legacyForwarder = _db.Queryable<ForwarderConfig>().Any(x => x.DeviceId == 0);
        if (!legacyParser && !legacyForwarder)
        {
            return;
        }

        var device = _db.Queryable<SerialDeviceConfig>().First();
        if (device == null)
        {
            return;
        }

        // 将旧的解析配置绑定到第一台设备
        _db.Updateable<ParserConfig>()
            .SetColumns(x => new ParserConfig { DeviceId = device.Id })
            .Where(x => x.DeviceId == 0)
            .ExecuteCommand();

        // 将旧的转发配置绑定到第一台设备
        _db.Updateable<ForwarderConfig>()
            .SetColumns(x => new ForwarderConfig { DeviceId = device.Id })
            .Where(x => x.DeviceId == 0)
            .ExecuteCommand();
    }
}
