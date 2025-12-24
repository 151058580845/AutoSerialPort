using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AutoSerialPort.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutoSerialPort.Application.Services;

/// <summary>
/// 线程安全的设备缓存实现
/// 提供高效的设备查找、变更跟踪和缓存同步功能
/// </summary>
public class DeviceCache : IDeviceCache
{
    private readonly ILogger<DeviceCache> _logger;
    private readonly object _lockObject = new();
    
    // 主缓存存储 - 使用ConcurrentDictionary确保线程安全
    private readonly ConcurrentDictionary<long, IDeviceProfileViewModel> _devicesById;
    private readonly ConcurrentDictionary<string, IDeviceProfileViewModel> _devicesByDisplayName;
    
    // 缓存版本号，用于变更跟踪
    private volatile int _cacheVersion;
    
    // 缓存统计信息
    private volatile int _totalDevices;
    
    public DeviceCache(ILogger<DeviceCache> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _devicesById = new ConcurrentDictionary<long, IDeviceProfileViewModel>();
        _devicesByDisplayName = new ConcurrentDictionary<string, IDeviceProfileViewModel>(StringComparer.OrdinalIgnoreCase);
        _cacheVersion = 0;
        _totalDevices = 0;
    }

    /// <summary>
    /// 缓存是否为空
    /// </summary>
    public bool IsEmpty => _totalDevices == 0;

    /// <summary>
    /// 获取当前缓存版本号（用于变更跟踪）
    /// </summary>
    public int CacheVersion => _cacheVersion;

    /// <summary>
    /// 获取所有缓存的设备
    /// </summary>
    /// <returns>只读设备列表</returns>
    public IReadOnlyList<IDeviceProfileViewModel> GetAll()
    {
        var operationContext = new CacheOperationContext
        {
            OperationType = "GetAll",
            StartTime = DateTime.UtcNow
        };

        try
        {
            // 从ID字典获取所有值，确保一致性
            var devices = _devicesById.Values.ToList();
            
            _logger.LogTrace("获取所有缓存设备，共 {Count} 个设备", devices.Count);
            
            operationContext.Result = "Success";
            operationContext.DeviceCount = devices.Count;
            LogOperationCompletion(operationContext, true);
            
            return devices.AsReadOnly();
        }
        catch (Exception ex)
        {
            HandleCacheOperationError(ex, operationContext, "获取所有缓存设备时发生错误");
            return new List<IDeviceProfileViewModel>().AsReadOnly();
        }
    }

    /// <summary>
    /// 根据ID获取设备
    /// </summary>
    /// <param name="id">设备ID</param>
    /// <returns>设备视图模型，如果未找到则返回null</returns>
    public IDeviceProfileViewModel? GetById(long id)
    {
        var operationContext = new CacheOperationContext
        {
            OperationType = "GetById",
            StartTime = DateTime.UtcNow,
            DeviceId = id
        };

        try
        {
            // 验证输入参数
            if (id <= 0)
            {
                _logger.LogTrace("无效的设备ID: {DeviceId}", id);
                operationContext.Result = "InvalidId";
                LogOperationCompletion(operationContext, false);
                return null;
            }

            var found = _devicesById.TryGetValue(id, out var device);
            
            _logger.LogTrace("根据ID {DeviceId} 查找设备: {Found}", id, found ? "找到" : "未找到");
            
            operationContext.Result = found ? "Found" : "NotFound";
            LogOperationCompletion(operationContext, found);
            
            return device;
        }
        catch (Exception ex)
        {
            HandleCacheOperationError(ex, operationContext, $"根据ID {id} 获取设备时发生错误");
            return null;
        }
    }

    /// <summary>
    /// 根据显示名称获取设备
    /// </summary>
    /// <param name="displayName">显示名称</param>
    /// <returns>设备视图模型，如果未找到则返回null</returns>
    public IDeviceProfileViewModel? GetByDisplayName(string displayName)
    {
        var operationContext = new CacheOperationContext
        {
            OperationType = "GetByDisplayName",
            StartTime = DateTime.UtcNow,
            DisplayName = displayName
        };

        if (string.IsNullOrWhiteSpace(displayName))
        {
            _logger.LogTrace("显示名称为空，返回null");
            operationContext.Result = "EmptyDisplayName";
            LogOperationCompletion(operationContext, false);
            return null;
        }

        try
        {
            var found = _devicesByDisplayName.TryGetValue(displayName, out var device);
            
            _logger.LogTrace("根据显示名称 '{DisplayName}' 查找设备: {Found}", displayName, found ? "找到" : "未找到");
            
            operationContext.Result = found ? "Found" : "NotFound";
            LogOperationCompletion(operationContext, found);
            
            return device;
        }
        catch (Exception ex)
        {
            HandleCacheOperationError(ex, operationContext, $"根据显示名称 '{displayName}' 获取设备时发生错误");
            return null;
        }
    }

    /// <summary>
    /// 更新缓存
    /// </summary>
    /// <param name="devices">设备集合</param>
    public void UpdateCache(IEnumerable<IDeviceProfileViewModel> devices)
    {
        var operationContext = new CacheOperationContext
        {
            OperationType = "UpdateCache",
            StartTime = DateTime.UtcNow
        };

        if (devices == null)
        {
            _logger.LogWarning("尝试使用null设备集合更新缓存");
            operationContext.Result = "NullDevices";
            LogOperationCompletion(operationContext, false);
            return;
        }

        lock (_lockObject)
        {
            try
            {
                _logger.LogDebug("开始更新设备缓存");
                
                // 验证缓存状态
                if (!ValidateCacheState(operationContext))
                {
                    AttemptCacheRecovery(operationContext);
                }
                
                // 清空现有缓存
                _devicesById.Clear();
                _devicesByDisplayName.Clear();
                
                var deviceList = devices.ToList();
                var addedCount = 0;
                var duplicateIds = new List<long>();
                var duplicateNames = new List<string>();
                var validationErrors = new List<string>();
                
                // 添加新设备到缓存
                foreach (var device in deviceList)
                {
                    if (device == null)
                    {
                        validationErrors.Add("Null device in collection");
                        _logger.LogWarning("跳过null设备");
                        continue;
                    }
                    
                    // 验证设备数据
                    if (!ValidateDeviceData(device, validationErrors))
                    {
                        continue;
                    }
                    
                    // 检查ID重复
                    if (!_devicesById.TryAdd(device.DeviceId, device))
                    {
                        duplicateIds.Add(device.DeviceId);
                        _logger.LogWarning("设备ID {DeviceId} 重复，跳过设备 '{DisplayName}'", 
                            device.DeviceId, device.DisplayName);
                        continue;
                    }
                    
                    // 检查显示名称重复
                    if (!string.IsNullOrWhiteSpace(device.DisplayName))
                    {
                        if (!_devicesByDisplayName.TryAdd(device.DisplayName, device))
                        {
                            duplicateNames.Add(device.DisplayName);
                            _logger.LogWarning("设备显示名称 '{DisplayName}' 重复，ID: {DeviceId}", 
                                device.DisplayName, device.DeviceId);
                            // 注意：显示名称重复不会阻止设备被添加到ID缓存中
                        }
                    }
                    else
                    {
                        validationErrors.Add($"Empty display name for device ID {device.DeviceId}");
                        _logger.LogWarning("设备 ID {DeviceId} 的显示名称为空", device.DeviceId);
                    }
                    
                    addedCount++;
                }
                
                // 更新统计信息和版本号
                _totalDevices = _devicesById.Count;
                _cacheVersion++;
                
                operationContext.DeviceCount = addedCount;
                operationContext.Result = "Success";
                operationContext.ValidationErrors.AddRange(validationErrors);
                
                _logger.LogInformation("缓存更新完成: 添加 {AddedCount} 个设备，总计 {TotalDevices} 个设备，版本号: {Version}", 
                    addedCount, _totalDevices, _cacheVersion);
                
                // 记录重复项统计
                if (duplicateIds.Count > 0)
                {
                    _logger.LogWarning("发现 {Count} 个重复的设备ID: {DuplicateIds}", 
                        duplicateIds.Count, string.Join(", ", duplicateIds));
                }
                
                if (duplicateNames.Count > 0)
                {
                    _logger.LogWarning("发现 {Count} 个重复的显示名称: {DuplicateNames}", 
                        duplicateNames.Count, string.Join(", ", duplicateNames.Select(n => $"'{n}'")));
                }
                
                // 验证更新后的缓存一致性
                var validationResult = ValidateConsistency();
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("缓存更新后一致性验证失败: {ValidationResult}", validationResult);
                    operationContext.ValidationErrors.Add($"Post-update validation failed: {validationResult}");
                }
                
                LogOperationCompletion(operationContext, true);
            }
            catch (Exception ex)
            {
                HandleCacheOperationError(ex, operationContext, "更新设备缓存时发生错误");
                
                // 发生错误时清空缓存以确保一致性
                try
                {
                    _devicesById.Clear();
                    _devicesByDisplayName.Clear();
                    _totalDevices = 0;
                    _cacheVersion++;
                    
                    _logger.LogWarning("由于错误，已清空缓存以确保一致性");
                }
                catch (Exception clearEx)
                {
                    _logger.LogError(clearEx, "清空缓存时发生错误");
                }
                
                throw;
            }
        }
    }

    /// <summary>
    /// 添加单个设备到缓存
    /// </summary>
    /// <param name="device">设备视图模型</param>
    /// <returns>是否添加成功</returns>
    public bool Add(IDeviceProfileViewModel device)
    {
        if (device == null)
        {
            _logger.LogWarning("尝试添加null设备到缓存");
            return false;
        }

        lock (_lockObject)
        {
            try
            {
                // 对于新设备（ID为0），使用显示名称作为临时键
                if (device.DeviceId <= 0)
                {
                    // 使用负数作为临时ID，基于当前时间戳
                    var tempId = -DateTime.UtcNow.Ticks;
                    _logger.LogDebug("设备ID无效，使用临时ID: {TempId}, 显示名称: {DisplayName}", tempId, device.DisplayName);
                }

                // 尝试添加到ID字典（允许ID为0的新设备）
                var idKey = device.DeviceId > 0 ? device.DeviceId : -DateTime.UtcNow.Ticks;
                if (!_devicesById.TryAdd(idKey, device))
                {
                    _logger.LogWarning("设备ID {DeviceId} 已存在于缓存中", device.DeviceId);
                    // 如果已存在，更新它
                    _devicesById[idKey] = device;
                }

                // 添加到显示名称字典
                if (!string.IsNullOrWhiteSpace(device.DisplayName))
                {
                    _devicesByDisplayName[device.DisplayName] = device;
                }

                _totalDevices = _devicesById.Count;
                _cacheVersion++;

                _logger.LogDebug("设备已添加到缓存: ID={DeviceId}, DisplayName={DisplayName}",
                    device.DeviceId, device.DisplayName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加设备到缓存时发生错误");
                return false;
            }
        }
    }

    /// <summary>
    /// 从缓存中移除设备
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <returns>是否移除成功</returns>
    public bool Remove(long deviceId)
    {
        lock (_lockObject)
        {
            try
            {
                if (_devicesById.TryRemove(deviceId, out var device))
                {
                    // 同时从显示名称字典中移除
                    if (device != null && !string.IsNullOrWhiteSpace(device.DisplayName))
                    {
                        _devicesByDisplayName.TryRemove(device.DisplayName, out _);
                    }

                    _totalDevices = _devicesById.Count;
                    _cacheVersion++;

                    _logger.LogDebug("设备已从缓存中移除: ID={DeviceId}", deviceId);
                    return true;
                }

                _logger.LogDebug("设备不在缓存中: ID={DeviceId}", deviceId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从缓存中移除设备时发生错误");
                return false;
            }
        }
    }

    /// <summary>
    /// 清空缓存
    /// </summary>
    public void Clear()
    {
        var operationContext = new CacheOperationContext
        {
            OperationType = "Clear",
            StartTime = DateTime.UtcNow
        };

        lock (_lockObject)
        {
            try
            {
                _logger.LogDebug("清空设备缓存");
                
                var previousCount = _totalDevices;
                
                _devicesById.Clear();
                _devicesByDisplayName.Clear();
                _totalDevices = 0;
                _cacheVersion++;
                
                operationContext.DeviceCount = previousCount;
                operationContext.Result = "Success";
                
                _logger.LogInformation("缓存已清空: 移除 {PreviousCount} 个设备，版本号: {Version}", 
                    previousCount, _cacheVersion);
                
                LogOperationCompletion(operationContext, true);
            }
            catch (Exception ex)
            {
                HandleCacheOperationError(ex, operationContext, "清空设备缓存时发生错误");
                throw;
            }
        }
    }

    /// <summary>
    /// 获取缓存统计信息（用于调试和监控）
    /// </summary>
    /// <returns>缓存统计信息</returns>
    public CacheStatistics GetStatistics()
    {
        return new CacheStatistics
        {
            TotalDevices = _totalDevices,
            CacheVersion = _cacheVersion,
            DevicesByIdCount = _devicesById.Count,
            DevicesByDisplayNameCount = _devicesByDisplayName.Count,
            IsEmpty = IsEmpty
        };
    }

    /// <summary>
    /// 验证缓存一致性（用于调试）
    /// </summary>
    /// <returns>一致性验证结果</returns>
    public CacheValidationResult ValidateConsistency()
    {
        lock (_lockObject)
        {
            var result = new CacheValidationResult();
            
            try
            {
                result.DevicesByIdCount = _devicesById.Count;
                result.DevicesByDisplayNameCount = _devicesByDisplayName.Count;
                result.TotalDevicesCount = _totalDevices;
                
                // 验证总数一致性
                result.IsCountConsistent = _devicesById.Count == _totalDevices;
                
                // 验证显示名称映射的一致性
                var inconsistentMappings = new List<string>();
                foreach (var kvp in _devicesByDisplayName)
                {
                    var displayName = kvp.Key;
                    var deviceFromDisplayName = kvp.Value;
                    var deviceFromId = _devicesById.GetValueOrDefault(deviceFromDisplayName.DeviceId);
                    
                    if (deviceFromId == null || !ReferenceEquals(deviceFromId, deviceFromDisplayName))
                    {
                        inconsistentMappings.Add(displayName);
                    }
                }
                
                result.InconsistentMappings = inconsistentMappings;
                result.IsMappingConsistent = inconsistentMappings.Count == 0;
                result.IsValid = result.IsCountConsistent && result.IsMappingConsistent;
                
                if (!result.IsValid)
                {
                    _logger.LogWarning("缓存一致性验证失败: {Result}", result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证缓存一致性时发生错误");
                result.ValidationError = ex.Message;
                result.IsValid = false;
            }
            
            return result;
        }
    }

    /// <summary>
    /// 验证缓存状态
    /// </summary>
    private bool ValidateCacheState(CacheOperationContext context)
    {
        try
        {
            // 检查字典是否为null
            if (_devicesById == null || _devicesByDisplayName == null)
            {
                context.ValidationErrors.Add("Cache dictionaries are null");
                return false;
            }

            // 检查锁对象
            if (_lockObject == null)
            {
                context.ValidationErrors.Add("Lock object is null");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证缓存状态时发生错误");
            context.ValidationErrors.Add($"Cache state validation failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 尝试缓存恢复
    /// </summary>
    private void AttemptCacheRecovery(CacheOperationContext context)
    {
        try
        {
            _logger.LogInformation("开始尝试缓存恢复");

            // 如果字典为null，尝试重新初始化
            if (_devicesById == null)
            {
                _logger.LogWarning("设备ID字典为null，尝试重新初始化");
                // 注意：这里无法重新初始化readonly字段，实际项目中需要不同的处理方式
                context.RecoveryErrors.Add("Cannot reinitialize readonly dictionary");
            }

            if (_devicesByDisplayName == null)
            {
                _logger.LogWarning("设备显示名称字典为null，尝试重新初始化");
                context.RecoveryErrors.Add("Cannot reinitialize readonly dictionary");
            }

            context.RecoveryAttempts++;
            _logger.LogInformation("缓存恢复完成，尝试次数: {Attempts}", context.RecoveryAttempts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "缓存恢复失败");
            context.RecoveryErrors.Add($"Cache recovery failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 验证设备数据
    /// </summary>
    private bool ValidateDeviceData(IDeviceProfileViewModel device, List<string> validationErrors)
    {
        try
        {
            // 验证设备ID
            if (device.DeviceId <= 0)
            {
                validationErrors.Add($"Invalid device ID: {device.DeviceId}");
                _logger.LogWarning("设备ID无效: {DeviceId}", device.DeviceId);
                return false;
            }

            // 验证显示名称
            if (string.IsNullOrWhiteSpace(device.DisplayName))
            {
                validationErrors.Add($"Empty display name for device ID: {device.DeviceId}");
                // 显示名称为空不阻止设备添加，只记录警告
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证设备数据时发生错误");
            validationErrors.Add($"Device validation failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 处理缓存操作错误
    /// </summary>
    private void HandleCacheOperationError(Exception ex, CacheOperationContext context, string message)
    {
        try
        {
            _logger.LogError(ex, message);
            
            context.Result = $"Error_{ex.GetType().Name}";
            context.ErrorMessage = ex.Message;
            
            // 根据异常类型执行不同的恢复策略
            switch (ex)
            {
                case ArgumentException argEx:
                    HandleArgumentError(argEx, context);
                    break;
                    
                case InvalidOperationException invalidOpEx:
                    HandleInvalidOperationError(invalidOpEx, context);
                    break;
                    
                case OutOfMemoryException memEx:
                    HandleOutOfMemoryError(memEx, context);
                    break;
                    
                default:
                    HandleGenericCacheError(ex, context);
                    break;
            }
            
            LogOperationCompletion(context, false);
        }
        catch (Exception handlingEx)
        {
            _logger.LogError(handlingEx, "处理缓存操作错误时发生异常");
        }
    }

    /// <summary>
    /// 处理参数错误
    /// </summary>
    private void HandleArgumentError(ArgumentException ex, CacheOperationContext context)
    {
        _logger.LogInformation("处理参数错误");
        // 参数错误通常是调用方问题，不需要特殊恢复
    }

    /// <summary>
    /// 处理无效操作错误
    /// </summary>
    private void HandleInvalidOperationError(InvalidOperationException ex, CacheOperationContext context)
    {
        _logger.LogInformation("处理无效操作错误，尝试缓存恢复");
        AttemptCacheRecovery(context);
    }

    /// <summary>
    /// 处理内存不足错误
    /// </summary>
    private void HandleOutOfMemoryError(OutOfMemoryException ex, CacheOperationContext context)
    {
        _logger.LogWarning("处理内存不足错误，清理缓存");
        
        try
        {
            // 清理缓存以释放内存
            _devicesById.Clear();
            _devicesByDisplayName.Clear();
            _totalDevices = 0;
            _cacheVersion++;
            
            _logger.LogInformation("由于内存不足，已清空缓存");
        }
        catch (Exception clearEx)
        {
            _logger.LogError(clearEx, "清理缓存时发生错误");
            context.RecoveryErrors.Add($"Cache cleanup failed: {clearEx.Message}");
        }
    }

    /// <summary>
    /// 处理通用缓存错误
    /// </summary>
    private void HandleGenericCacheError(Exception ex, CacheOperationContext context)
    {
        _logger.LogInformation("处理通用缓存错误");
        
        // 对于未知错误，尝试验证缓存一致性
        try
        {
            var validationResult = ValidateConsistency();
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("检测到缓存不一致，尝试修复");
                // 这里可以添加自动修复逻辑
            }
        }
        catch (Exception validationEx)
        {
            _logger.LogWarning(validationEx, "缓存一致性验证失败");
            context.RecoveryErrors.Add($"Consistency validation failed: {validationEx.Message}");
        }
    }

    /// <summary>
    /// 记录操作完成信息
    /// </summary>
    private void LogOperationCompletion(CacheOperationContext context, bool success)
    {
        try
        {
            var duration = DateTime.UtcNow - context.StartTime;
            
            var logLevel = success ? LogLevel.Trace : LogLevel.Warning;
            _logger.Log(logLevel,
                "缓存操作完成 - 类型: {OperationType}, 结果: {Result}, 耗时: {Duration}ms, 设备数量: {DeviceCount}, 恢复尝试: {RecoveryAttempts}",
                context.OperationType, context.Result, duration.TotalMilliseconds,
                context.DeviceCount, context.RecoveryAttempts);

            if (!success)
            {
                if (context.ValidationErrors.Count > 0)
                {
                    _logger.LogWarning("验证错误: {ValidationErrors}", string.Join(", ", context.ValidationErrors));
                }

                if (context.RecoveryErrors.Count > 0)
                {
                    _logger.LogWarning("恢复错误: {RecoveryErrors}", string.Join(", ", context.RecoveryErrors));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录缓存操作完成信息时发生错误");
        }
    }
}

/// <summary>
/// 缓存统计信息
/// </summary>
public class CacheStatistics
{
    public int TotalDevices { get; init; }
    public int CacheVersion { get; init; }
    public int DevicesByIdCount { get; init; }
    public int DevicesByDisplayNameCount { get; init; }
    public bool IsEmpty { get; init; }
}

/// <summary>
/// 缓存一致性验证结果
/// </summary>
public class CacheValidationResult
{
    public int DevicesByIdCount { get; set; }
    public int DevicesByDisplayNameCount { get; set; }
    public int TotalDevicesCount { get; set; }
    public bool IsCountConsistent { get; set; }
    public bool IsMappingConsistent { get; set; }
    public List<string> InconsistentMappings { get; set; } = new();
    public bool IsValid { get; set; }
    public string? ValidationError { get; set; }
    
    public override string ToString()
    {
        return $"DevicesByIdCount: {DevicesByIdCount}, " +
               $"DevicesByDisplayNameCount: {DevicesByDisplayNameCount}, " +
               $"TotalDevicesCount: {TotalDevicesCount}, " +
               $"IsCountConsistent: {IsCountConsistent}, " +
               $"IsMappingConsistent: {IsMappingConsistent}, " +
               $"InconsistentMappings: [{string.Join(", ", InconsistentMappings)}], " +
               $"IsValid: {IsValid}, " +
               $"ValidationError: {ValidationError}";
    }
}

/// <summary>
/// 缓存操作上下文，用于跟踪操作状态和错误恢复
/// </summary>
internal class CacheOperationContext
{
    public string OperationType { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public long? DeviceId { get; set; }
    public string? DisplayName { get; set; }
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public int DeviceCount { get; set; }
    public int RecoveryAttempts { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> RecoveryErrors { get; set; } = new();
}