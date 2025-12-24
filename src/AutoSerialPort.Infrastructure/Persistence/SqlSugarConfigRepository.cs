using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Entities;
using AutoSerialPort.Domain.Models;
using SqlSugar;

namespace AutoSerialPort.Infrastructure.Persistence;

/// <summary>
/// SQLSugar 配置仓储实现。
/// </summary>
public class SqlSugarConfigRepository : IConfigRepository
{
    private readonly SqlSugarScope _db;

    /// <summary>
    /// 创建配置仓储。
    /// </summary>
    /// <param name="db">数据库上下文。</param>
    public SqlSugarConfigRepository(SqlSugarScope db)
    {
        _db = db;
    }

    /// <summary>
    /// 获取应用设置，不存在则创建默认记录。
    /// </summary>
    public async Task<AppSettings> GetAppSettingsAsync()
    {
        var settings = await _db.Queryable<AppSettings>().FirstAsync();
        if (settings == null)
        {
            settings = new AppSettings();
            await _db.Insertable(settings).ExecuteCommandAsync();
        }

        return settings;
    }

    /// <summary>
    /// 保存应用设置。
    /// </summary>
    /// <param name="settings">应用设置。</param>
    public Task SaveAppSettingsAsync(AppSettings settings)
        => _db.Storageable(settings).ExecuteCommandAsync();

    /// <summary>
    /// 获取指定方案下的设备配置集合。
    /// </summary>
    /// <param name="profileId">方案 Id。</param>
    public async Task<SerialDeviceProfile[]> GetSerialDeviceProfilesAsync(long profileId)
    {
        var devices = await _db.Queryable<SerialDeviceConfig>().Where(x => x.ProfileId == profileId).ToListAsync();
        var profiles = new List<SerialDeviceProfile>();

        foreach (var device in devices)
        {
            // 确保子配置完整
            var parser = await EnsureParserAsync(profileId, device.Id);
            var frameDecoder = await EnsureFrameDecoderAsync(profileId, device.Id);
            var forwarders = await EnsureForwardersAsync(profileId, device.Id);

            profiles.Add(new SerialDeviceProfile
            {
                Serial = device,
                Parser = parser,
                FrameDecoder = frameDecoder,
                Forwarders = forwarders.ToArray()
            });
        }

        return profiles.ToArray();
    }

    /// <summary>
    /// 保存指定方案下的设备配置集合。
    /// </summary>
    /// <param name="profileId">方案 Id。</param>
    /// <param name="profiles">设备配置集合。</param>
    public async Task SaveSerialDeviceProfilesAsync(long profileId, SerialDeviceProfile[] profiles)
    {
        await _db.Ado.UseTranAsync(async () =>
        {
            var existingDeviceIds = await _db.Queryable<SerialDeviceConfig>()
                .Where(x => x.ProfileId == profileId)
                .Select(x => x.Id)
                .ToListAsync();

            var incomingIds = profiles.Select(x => x.Serial.Id).Where(x => x > 0).ToList();
            var removed = existingDeviceIds.Except(incomingIds).ToList();
            if (removed.Count > 0)
            {
                // 删除已移除设备的所有子配置
                await _db.Deleteable<ParserConfig>().Where(x => removed.Contains(x.DeviceId)).ExecuteCommandAsync();
                await _db.Deleteable<FrameDecoderConfig>().Where(x => removed.Contains(x.DeviceId))
                    .ExecuteCommandAsync();
                await _db.Deleteable<ForwarderConfig>().Where(x => removed.Contains(x.DeviceId)).ExecuteCommandAsync();
                await _db.Deleteable<SerialDeviceConfig>().Where(x => removed.Contains(x.Id)).ExecuteCommandAsync();
            }

            foreach (var profile in profiles)
            {
                // 保存设备配置
                if (profile.Serial.Id == 0)
                {
                    var newId = await _db.Insertable(profile.Serial).ExecuteReturnIdentityAsync();
                    profile.Serial.Id = newId;
                }
                else
                {
                    await _db.Storageable(profile.Serial).ExecuteCommandAsync();
                }

                // 保存解析器配置
                var parser = profile.Parser;
                parser.ProfileId = profileId;
                parser.DeviceId = profile.Serial.Id;
                if (parser.Id == 0)
                {
                    var newId = await _db.Insertable(parser).ExecuteReturnIdentityAsync();
                    parser.Id = newId;
                }
                else
                {
                    await _db.Storageable(parser).ExecuteCommandAsync();
                }

                // 保存拆包配置
                var frameDecoder = profile.FrameDecoder;
                frameDecoder.ProfileId = profileId;
                frameDecoder.DeviceId = profile.Serial.Id;
                if (frameDecoder.Id == 0)
                {
                    var newId = await _db.Insertable(frameDecoder).ExecuteReturnIdentityAsync();
                    frameDecoder.Id = newId;
                }
                else
                {
                    await _db.Storageable(frameDecoder).ExecuteCommandAsync();
                }

                // 保存转发器配置
                var forwarders = profile.Forwarders;
                foreach (var forwarder in forwarders)
                {
                    forwarder.ProfileId = profileId;
                    forwarder.DeviceId = profile.Serial.Id;
                    if (forwarder.Id == 0)
                    {
                        var newId = await _db.Insertable(forwarder).ExecuteReturnIdentityAsync();
                        forwarder.Id = newId;
                    }
                    else
                    {
                        await _db.Storageable(forwarder).ExecuteCommandAsync();
                    }
                }

                // 删除未提交的转发器记录
                var forwarderIds = forwarders.Select(x => x.Id).Where(x => x > 0).ToList();
                await _db.Deleteable<ForwarderConfig>()
                    .Where(x => x.ProfileId == profileId && x.DeviceId == profile.Serial.Id &&
                                !forwarderIds.Contains(x.Id))
                    .ExecuteCommandAsync();
            }
        });
    }

    /// <summary>
    /// 获取方案列表。
    /// </summary>
    public async Task<Profile[]> GetProfilesAsync()
    {
        var profiles = await _db.Queryable<Profile>().ToListAsync();
        return profiles.ToArray();
    }

    /// <summary>
    /// 确保默认方案存在并返回其 Id。
    /// </summary>
    public async Task<long> EnsureDefaultProfileAsync()
    {
        var profiles = await _db.Queryable<Profile>().ToListAsync();
        if (profiles.Count == 0)
        {
            var id = await _db.Insertable(new Profile { Name = "Default" }).ExecuteReturnIdentityAsync();
            return id;
        }

        return profiles.First().Id;
    }

    /// <summary>
    /// 确保解析器配置存在。
    /// </summary>
    private async Task<ParserConfig> EnsureParserAsync(long profileId, long deviceId)
    {
        var config = await _db.Queryable<ParserConfig>()
            .FirstAsync(x => x.ProfileId == profileId && x.DeviceId == deviceId);
        if (config != null)
        {
            return config;
        }

        var parser = new ParserConfig
        {
            ProfileId = profileId,
            DeviceId = deviceId,
            ParserType = "LineParser",
            ParametersJson = "{\"encoding\":\"utf-8\",\"separator\":\"\\n\"}"
        };
        await _db.Insertable(parser).ExecuteCommandAsync();
        return parser;
    }

    /// <summary>
    /// 确保转发器配置存在。
    /// </summary>
    private async Task<List<ForwarderConfig>> EnsureForwardersAsync(long profileId, long deviceId)
    {
        var configs = await _db.Queryable<ForwarderConfig>()
            .Where(x => x.ProfileId == profileId && x.DeviceId == deviceId)
            .ToListAsync();
        if (configs.Count > 0)
        {
            return configs;
        }

        configs = new List<ForwarderConfig>
        {
            new()
            {
                ProfileId = profileId,
                DeviceId = deviceId,
                ForwarderType = "TcpForwarder",
                IsEnabled = false,
                ParametersJson = "{\"mode\":\"Server\",\"host\":\"0.0.0.0\",\"port\":9000}"
            },
            new()
            {
                ProfileId = profileId,
                DeviceId = deviceId,
                ForwarderType = "MqttForwarder",
                IsEnabled = false,
                ParametersJson =
                    "{\"broker\":\"localhost\",\"port\":1883,\"topic\":\"demo/topic\",\"qos\":0,\"retain\":false}"
            },
            new()
            {
                ProfileId = profileId,
                DeviceId = deviceId,
                ForwarderType = "ClipboardForwarder",
                IsEnabled = false,
                ParametersJson = "{\"appendNewLine\":false}"
            },
            new()
            {
                ProfileId = profileId,
                DeviceId = deviceId,
                ForwarderType = "TypingForwarder",
                IsEnabled = false,
                ParametersJson = "{\"delayMs\":0}"
            }
        };

        await _db.Insertable(configs).ExecuteCommandAsync();
        return configs;
    }

    /// <summary>
    /// 确保拆包配置存在。
    /// </summary>
    private async Task<FrameDecoderConfig> EnsureFrameDecoderAsync(long profileId, long deviceId)
    {
        var config = await _db.Queryable<FrameDecoderConfig>()
            .FirstAsync(x => x.ProfileId == profileId && x.DeviceId == deviceId);
        if (config != null)
        {
            return config;
        }

        var decoder = new FrameDecoderConfig
        {
            ProfileId = profileId,
            DeviceId = deviceId,
            DecoderType = "DelimiterFrameDecoder",
            ParametersJson =
                "{\"encoding\":\"utf-8\",\"delimiter\":\"\\n\",\"includeDelimiter\":true,\"maxBufferLength\":65536}"
        };
        await _db.Insertable(decoder).ExecuteCommandAsync();
        return decoder;
    }
}
