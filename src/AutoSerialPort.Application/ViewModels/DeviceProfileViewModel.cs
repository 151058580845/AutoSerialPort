using AutoSerialPort.Application.Abstractions;
using AutoSerialPort.Domain.Models;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace AutoSerialPort.Application.ViewModels;

/// <summary>
/// 设备配置视图模型实现
/// 提供设备配置的数据绑定和状态管理功能
/// 这是一个轻量级实现，专注于数据管理而不包含UI特定逻辑
/// </summary>
public class DeviceProfileViewModel : IDeviceProfileViewModel, INotifyPropertyChanged
{
    private readonly ILogger<DeviceProfileViewModel>? _logger;
    private SerialDeviceProfile? _profile;
    private SerialDeviceStatus? _status;
    private bool _isRunning;

    /// <summary>
    /// 属性变更事件
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 设备ID
    /// </summary>
    public long DeviceId { get; private set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// 是否已启用
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// 是否正在运行
    /// </summary>
    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (_isRunning != value)
            {
                _isRunning = value;
                OnPropertyChanged(nameof(IsRunning));
            }
        }
    }

    /// <summary>
    /// 串口设置（Application层实现返回null）
    /// </summary>
    public ISerialSettingsViewModel? SerialSettings => null;

    /// <summary>
    /// 解析器设置（Application层实现返回null）
    /// </summary>
    public IParserSettingsViewModel? ParserSettings => null;

    /// <summary>
    /// 转发器设置（Application层实现返回null）
    /// </summary>
    public IForwarderSettingsViewModel? ForwarderSettings => null;

    /// <summary>
    /// 当前设备状态（只读）
    /// </summary>
    public SerialDeviceStatus? Status => _status;

    /// <summary>
    /// 当前设备配置（只读）
    /// </summary>
    public SerialDeviceProfile? Profile => _profile;

    /// <summary>
    /// 创建设备配置视图模型实例
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public DeviceProfileViewModel(ILogger<DeviceProfileViewModel>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// 加载设备配置
    /// </summary>
    /// <param name="profile">设备配置</param>
    public void Load(SerialDeviceProfile profile)
    {
        try
        {
            if (profile == null)
            {
                _logger?.LogWarning("尝试加载空的设备配置");
                return;
            }

            _profile = profile;
            
            // 更新基本属性
            var previousDeviceId = DeviceId;
            var previousDisplayName = DisplayName;
            var previousIsEnabled = IsEnabled;

            DeviceId = profile.Serial.Id;
            DisplayName = !string.IsNullOrWhiteSpace(profile.Serial.DisplayName) 
                ? profile.Serial.DisplayName 
                : $"设备 {profile.Serial.Id}";
            IsEnabled = profile.Serial.IsEnabled;

            // 触发属性变更通知
            if (previousDeviceId != DeviceId)
                OnPropertyChanged(nameof(DeviceId));
            if (previousDisplayName != DisplayName)
                OnPropertyChanged(nameof(DisplayName));
            if (previousIsEnabled != IsEnabled)
                OnPropertyChanged(nameof(IsEnabled));

            _logger?.LogDebug("已加载设备配置: ID={DeviceId}, DisplayName={DisplayName}, IsEnabled={IsEnabled}", 
                DeviceId, DisplayName, IsEnabled);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "加载设备配置时发生错误: DeviceId={DeviceId}", profile?.Serial?.Id);
            throw;
        }
    }

    /// <summary>
    /// 构建设备配置
    /// </summary>
    /// <returns>设备配置</returns>
    public SerialDeviceProfile BuildProfile()
    {
        try
        {
            if (_profile == null)
            {
                _logger?.LogWarning("尝试构建配置但当前没有加载的配置，返回默认配置");
                return CreateDefaultProfile();
            }

            // 返回当前配置的副本，确保不会意外修改原始数据
            var profile = new SerialDeviceProfile
            {
                Serial = CloneSerialConfig(_profile.Serial),
                Parser = CloneParserConfig(_profile.Parser),
                FrameDecoder = CloneFrameDecoderConfig(_profile.FrameDecoder),
                Forwarders = CloneForwarderConfigs(_profile.Forwarders)
            };

            _logger?.LogDebug("已构建设备配置: ID={DeviceId}, DisplayName={DisplayName}", 
                DeviceId, DisplayName);

            return profile;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "构建设备配置时发生错误: DeviceId={DeviceId}", DeviceId);
            throw;
        }
    }

    /// <summary>
    /// 更新运行状态
    /// </summary>
    /// <param name="status">设备状态</param>
    public void UpdateStatus(SerialDeviceStatus? status)
    {
        try
        {
            _status = status;
            
            var previousIsRunning = IsRunning;
            IsRunning = status?.IsRunning ?? false;

            // 如果运行状态发生变化，记录日志
            if (previousIsRunning != IsRunning)
            {
                _logger?.LogDebug("设备运行状态已更新: DeviceId={DeviceId}, IsRunning={IsRunning}", 
                    DeviceId, IsRunning);
            }

            // 如果状态不为空，验证设备ID是否匹配
            if (status != null && status.DeviceId != DeviceId)
            {
                _logger?.LogWarning("状态更新中的设备ID不匹配: 期望={ExpectedDeviceId}, 实际={ActualDeviceId}", 
                    DeviceId, status.DeviceId);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "更新设备状态时发生错误: DeviceId={DeviceId}", DeviceId);
            throw;
        }
    }

    /// <summary>
    /// 触发属性变更通知
    /// </summary>
    /// <param name="propertyName">属性名称</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 创建默认设备配置
    /// </summary>
    /// <returns>默认设备配置</returns>
    private SerialDeviceProfile CreateDefaultProfile()
    {
        return new SerialDeviceProfile
        {
            Serial = new Domain.Entities.SerialDeviceConfig
            {
                Id = DeviceId,
                DisplayName = DisplayName,
                IsEnabled = IsEnabled
            },
            Parser = new Domain.Entities.ParserConfig(),
            FrameDecoder = new Domain.Entities.FrameDecoderConfig(),
            Forwarders = Array.Empty<Domain.Entities.ForwarderConfig>()
        };
    }

    /// <summary>
    /// 克隆串口配置
    /// </summary>
    private Domain.Entities.SerialDeviceConfig CloneSerialConfig(Domain.Entities.SerialDeviceConfig original)
    {
        return new Domain.Entities.SerialDeviceConfig
        {
            Id = original.Id,
            ProfileId = original.ProfileId,
            DisplayName = original.DisplayName,
            IdentifierType = original.IdentifierType,
            IdentifierValue = original.IdentifierValue,
            BaudRate = original.BaudRate,
            Parity = original.Parity,
            DataBits = original.DataBits,
            StopBits = original.StopBits,
            IsEnabled = original.IsEnabled
        };
    }

    /// <summary>
    /// 克隆解析器配置
    /// </summary>
    private Domain.Entities.ParserConfig CloneParserConfig(Domain.Entities.ParserConfig original)
    {
        return new Domain.Entities.ParserConfig
        {
            Id = original.Id,
            ProfileId = original.ProfileId,
            DeviceId = original.DeviceId,
            ParserType = original.ParserType,
            ParametersJson = original.ParametersJson
        };
    }

    /// <summary>
    /// 克隆帧解码器配置
    /// </summary>
    private Domain.Entities.FrameDecoderConfig CloneFrameDecoderConfig(Domain.Entities.FrameDecoderConfig original)
    {
        return new Domain.Entities.FrameDecoderConfig
        {
            Id = original.Id,
            ProfileId = original.ProfileId,
            DeviceId = original.DeviceId,
            DecoderType = original.DecoderType,
            ParametersJson = original.ParametersJson
        };
    }

    /// <summary>
    /// 克隆转发器配置数组
    /// </summary>
    private Domain.Entities.ForwarderConfig[] CloneForwarderConfigs(Domain.Entities.ForwarderConfig[] original)
    {
        if (original == null || original.Length == 0)
            return Array.Empty<Domain.Entities.ForwarderConfig>();

        var cloned = new Domain.Entities.ForwarderConfig[original.Length];
        for (int i = 0; i < original.Length; i++)
        {
            cloned[i] = new Domain.Entities.ForwarderConfig
            {
                Id = original[i].Id,
                ProfileId = original[i].ProfileId,
                DeviceId = original[i].DeviceId,
                ForwarderType = original[i].ForwarderType,
                IsEnabled = original[i].IsEnabled,
                ParametersJson = original[i].ParametersJson
            };
        }
        return cloned;
    }

    /// <summary>
    /// 重写ToString以便于调试
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return $"DeviceProfileViewModel(Id={DeviceId}, DisplayName='{DisplayName}', IsEnabled={IsEnabled}, IsRunning={IsRunning})";
    }
}