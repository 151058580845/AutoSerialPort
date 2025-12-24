# 设备选择状态管理故障排除指南

## 快速诊断检查清单

### 1. 基本状态检查
```csharp
// 检查系统基本状态
var isLoading = deviceDataManager.IsLoading;
var deviceCount = deviceDataManager.Devices.Count;
var selectedDevice = selectionStateManager.SelectedDevice;
var isSelectionValid = selectionStateManager.IsSelectionValid();

logger.LogInformation("系统状态 - 加载中: {IsLoading}, 设备数: {DeviceCount}, 选中设备: {SelectedDevice}, 选择有效: {IsValid}",
    isLoading, deviceCount, selectedDevice?.DisplayName ?? "无", isSelectionValid);
```

### 2. 缓存状态检查
```csharp
// 检查缓存状态
var cacheStats = deviceCache.GetStatistics();
var cacheValidation = deviceCache.ValidateConsistency();

logger.LogInformation("缓存状态: {Stats}", cacheStats);
if (!cacheValidation.IsValid) {
    logger.LogWarning("缓存一致性问题: {Validation}", cacheValidation);
}
```

### 3. 数据管理器状态检查
```csharp
// 检查数据管理器状态
var dataStats = deviceDataManager.GetStatistics();
logger.LogInformation("数据管理器状态: {Stats}", dataStats);
```

## 常见问题诊断

### 问题1: 设备选择无响应

#### 症状
- 用户点击设备列表项无反应
- UI不更新选中状态
- 右侧面板不显示设备信息

#### 可能原因
1. **系统忙碌状态**: `IsBusy` 为 true
2. **并发操作冲突**: 多个选择操作同时进行
3. **设备不在缓存中**: 缓存数据不一致
4. **UI线程阻塞**: 长时间操作阻塞UI

#### 诊断步骤
```csharp
// 1. 检查忙碌状态
if (mainWindowViewModel.IsBusy) {
    logger.LogDebug("系统忙碌中，IsLoading: {IsLoading}, DataManagerLoading: {DataManagerLoading}",
        mainWindowViewModel.IsLoading, deviceDataManager.IsLoading);
}

// 2. 检查设备是否存在
var targetDevice = deviceCache.GetById(deviceId);
if (targetDevice == null) {
    logger.LogWarning("目标设备不在缓存中: {DeviceId}", deviceId);
}

// 3. 检查选择操作日志
// 查看日志中是否有 "开始选择设备" 但没有对应的 "设备选择成功" 或 "设备选择失败"
```

#### 解决方案
```csharp
// 1. 等待当前操作完成
while (mainWindowViewModel.IsBusy) {
    await Task.Delay(100);
}

// 2. 强制刷新缓存
await deviceDataManager.RefreshDataAsync(preserveSelection: false);

// 3. 重置选择状态
selectionStateManager.ClearSelection();

// 4. 重新尝试选择
await selectionStateManager.SelectDeviceAsync(deviceId);
```

### 问题2: 数据加载失败

#### 症状
- 设备列表为空
- 显示"暂无设备"状态
- 预加载操作失败

#### 可能原因
1. **数据库连接问题**: 无法连接到SQLite数据库
2. **配置文件损坏**: 数据库文件损坏或格式错误
3. **权限问题**: 无法访问数据文件
4. **默认配置缺失**: 缺少默认配置文件

#### 诊断步骤
```csharp
// 1. 检查数据库连接
try {
    var profileId = await configRepository.EnsureDefaultProfileAsync();
    logger.LogInformation("默认配置文件ID: {ProfileId}", profileId);
} catch (Exception ex) {
    logger.LogError(ex, "数据库连接失败");
}

// 2. 检查配置文件
try {
    var profiles = await configRepository.GetSerialDeviceProfilesAsync(1);
    logger.LogInformation("加载到 {Count} 个设备配置", profiles.Length);
} catch (Exception ex) {
    logger.LogError(ex, "加载设备配置失败");
}

// 3. 检查文件权限
var dbPath = Path.Combine(appPathService.DataDirectory, "config.db");
var fileExists = File.Exists(dbPath);
var canRead = fileExists && new FileInfo(dbPath).IsReadOnly == false;
logger.LogInformation("数据库文件 - 存在: {Exists}, 可读写: {CanRead}, 路径: {Path}",
    fileExists, canRead, dbPath);
```

#### 解决方案
```csharp
// 1. 重新初始化数据库
var initializer = serviceProvider.GetRequiredService<SqlSugarInitializer>();
await initializer.InitializeAsync();

// 2. 创建默认配置
if (deviceDataManager.Devices.Count == 0) {
    // 添加默认设备
    mainWindowViewModel.AddDeviceCommand.Execute(null);
}

// 3. 备份和恢复数据库
var backupPath = dbPath + ".backup";
if (File.Exists(backupPath)) {
    File.Copy(backupPath, dbPath, overwrite: true);
    await deviceDataManager.RefreshDataAsync();
}
```

### 问题3: 选择状态不一致

#### 症状
- UI显示选中设备A，但右侧面板显示设备B的信息
- `SelectedDevice` 属性与实际选中项不符
- 选择验证失败

#### 可能原因
1. **缓存不一致**: 缓存中的设备信息与UI不同步
2. **事件处理延迟**: SelectionChanged事件处理延迟
3. **并发更新冲突**: 多个组件同时更新选择状态
4. **ViewModel状态损坏**: DeviceProfileViewModel状态不正确

#### 诊断步骤
```csharp
// 1. 比较各组件的选择状态
var uiSelected = mainWindowViewModel.SelectedDevice;
var managerSelected = selectionStateManager.SelectedDevice;
var isValid = selectionStateManager.IsSelectionValid();

logger.LogWarning("选择状态不一致 - UI: {UIDevice}, Manager: {ManagerDevice}, Valid: {IsValid}",
    uiSelected?.DeviceId, managerSelected?.DeviceId, isValid);

// 2. 检查缓存一致性
var validation = deviceCache.ValidateConsistency();
if (!validation.IsValid) {
    logger.LogError("缓存一致性验证失败: {Validation}", validation);
}

// 3. 验证设备引用
if (uiSelected != null && managerSelected != null) {
    var sameReference = ReferenceEquals(uiSelected, managerSelected);
    var sameId = uiSelected.DeviceId == managerSelected.DeviceId;
    logger.LogDebug("设备引用比较 - 相同引用: {SameRef}, 相同ID: {SameId}",
        sameReference, sameId);
}
```

#### 解决方案
```csharp
// 1. 强制同步选择状态
var targetDevice = selectionStateManager.SelectedDevice;
if (targetDevice != null) {
    await Dispatcher.UIThread.InvokeAsync(() => {
        mainWindowViewModel.SelectedDevice = targetDevice;
    });
}

// 2. 重建缓存
await deviceDataManager.RefreshDataAsync(preserveSelection: true);

// 3. 重置并重新选择
selectionStateManager.ClearSelection();
if (targetDevice != null) {
    await selectionStateManager.SelectDeviceAsync(targetDevice.DeviceId);
}
```

### 问题4: 性能问题

#### 症状
- 设备选择响应缓慢（>1秒）
- 数据加载时间过长
- UI冻结或卡顿

#### 可能原因
1. **设备数量过多**: 大量设备导致性能下降
2. **缓存未命中**: 频繁的数据库查询
3. **UI线程阻塞**: 在UI线程执行耗时操作
4. **内存不足**: 系统内存压力大

#### 诊断步骤
```csharp
// 1. 性能计时
var stopwatch = Stopwatch.StartNew();
await selectionStateManager.SelectDeviceAsync(deviceId);
stopwatch.Stop();
logger.LogWarning("设备选择耗时: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

// 2. 检查设备数量
var deviceCount = deviceDataManager.Devices.Count;
if (deviceCount > 1000) {
    logger.LogWarning("设备数量过多: {Count}", deviceCount);
}

// 3. 检查缓存命中率
var cacheStats = deviceCache.GetStatistics();
logger.LogInformation("缓存统计: {Stats}", cacheStats);

// 4. 内存使用检查
var memoryBefore = GC.GetTotalMemory(false);
// 执行操作
var memoryAfter = GC.GetTotalMemory(false);
var memoryUsed = memoryAfter - memoryBefore;
logger.LogInformation("内存使用: {MemoryUsed} bytes", memoryUsed);
```

#### 解决方案
```csharp
// 1. 启用缓存优化
deviceDataManager.SetBackgroundRefreshEnabled(true);

// 2. 分批处理大量设备
if (deviceCount > 500) {
    // 考虑实现虚拟化或分页
    logger.LogInformation("建议实现设备列表虚拟化");
}

// 3. 异步操作优化
await Task.Run(async () => {
    // 在后台线程执行耗时操作
    await deviceDataManager.PreloadDataAsync();
});

// 4. 内存清理
GC.Collect();
GC.WaitForPendingFinalizers();
```

## 高级诊断技术

### 1. 启用详细日志记录

#### 环境变量方式
```bash
# Windows Command Prompt
set AUTOSERIAL_DEBUG=true
AutoSerialPort.Host.exe

# Windows PowerShell
$env:AUTOSERIAL_DEBUG="true"
.\AutoSerialPort.Host.exe

# 或使用命令行参数
AutoSerialPort.Host.exe --debug
```

#### 程序内启用
```csharp
// 在Program.cs中添加
Environment.SetEnvironmentVariable("AUTOSERIAL_DEBUG", "true");
```

### 2. 实时状态监控

```csharp
// 创建状态监控器
public class DeviceSelectionMonitor
{
    private readonly Timer _monitorTimer;
    private readonly ILogger _logger;
    
    public DeviceSelectionMonitor(ILogger logger)
    {
        _logger = logger;
        _monitorTimer = new Timer(LogSystemStatus, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }
    
    private void LogSystemStatus(object? state)
    {
        try
        {
            var stats = new
            {
                DeviceCount = deviceDataManager.Devices.Count,
                IsLoading = deviceDataManager.IsLoading,
                SelectedDevice = selectionStateManager.SelectedDevice?.DeviceId,
                IsSelectionValid = selectionStateManager.IsSelectionValid(),
                CacheStats = deviceCache.GetStatistics(),
                Timestamp = DateTime.UtcNow
            };
            
            _logger.LogDebug("系统状态监控: {@Stats}", stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "状态监控失败");
        }
    }
}
```

### 3. 性能分析工具

```csharp
// 性能分析器
public class PerformanceAnalyzer
{
    private readonly Dictionary<string, List<long>> _operationTimes = new();
    private readonly ILogger _logger;
    
    public IDisposable MeasureOperation(string operationName)
    {
        return new OperationMeasurement(operationName, this);
    }
    
    private class OperationMeasurement : IDisposable
    {
        private readonly Stopwatch _stopwatch;
        private readonly string _operationName;
        private readonly PerformanceAnalyzer _analyzer;
        
        public OperationMeasurement(string operationName, PerformanceAnalyzer analyzer)
        {
            _operationName = operationName;
            _analyzer = analyzer;
            _stopwatch = Stopwatch.StartNew();
        }
        
        public void Dispose()
        {
            _stopwatch.Stop();
            _analyzer.RecordOperation(_operationName, _stopwatch.ElapsedMilliseconds);
        }
    }
    
    private void RecordOperation(string operationName, long elapsedMs)
    {
        if (!_operationTimes.ContainsKey(operationName))
        {
            _operationTimes[operationName] = new List<long>();
        }
        
        _operationTimes[operationName].Add(elapsedMs);
        
        // 记录慢操作
        if (elapsedMs > 100)
        {
            _logger.LogWarning("慢操作检测: {Operation} 耗时 {ElapsedMs}ms", operationName, elapsedMs);
        }
    }
    
    public void LogStatistics()
    {
        foreach (var kvp in _operationTimes)
        {
            var times = kvp.Value;
            var avg = times.Average();
            var max = times.Max();
            var min = times.Min();
            
            _logger.LogInformation("操作统计 {Operation}: 平均 {Avg:F2}ms, 最大 {Max}ms, 最小 {Min}ms, 次数 {Count}",
                kvp.Key, avg, max, min, times.Count);
        }
    }
}

// 使用示例
using (performanceAnalyzer.MeasureOperation("DeviceSelection"))
{
    await selectionStateManager.SelectDeviceAsync(deviceId);
}
```

### 4. 内存泄漏检测

```csharp
// 内存监控器
public class MemoryMonitor
{
    private readonly Timer _memoryTimer;
    private readonly ILogger _logger;
    private long _lastMemoryUsage;
    
    public MemoryMonitor(ILogger logger)
    {
        _logger = logger;
        _lastMemoryUsage = GC.GetTotalMemory(false);
        _memoryTimer = new Timer(CheckMemoryUsage, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }
    
    private void CheckMemoryUsage(object? state)
    {
        var currentMemory = GC.GetTotalMemory(false);
        var memoryDiff = currentMemory - _lastMemoryUsage;
        
        _logger.LogDebug("内存使用: 当前 {Current:N0} bytes, 变化 {Diff:N0} bytes",
            currentMemory, memoryDiff);
        
        if (memoryDiff > 10_000_000) // 10MB增长
        {
            _logger.LogWarning("内存使用快速增长: {Diff:N0} bytes", memoryDiff);
            
            // 强制垃圾回收并重新检查
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var afterGC = GC.GetTotalMemory(false);
            _logger.LogInformation("垃圾回收后内存: {AfterGC:N0} bytes, 释放 {Released:N0} bytes",
                afterGC, currentMemory - afterGC);
        }
        
        _lastMemoryUsage = currentMemory;
    }
}
```

## 预防性维护

### 1. 定期健康检查

```csharp
// 系统健康检查
public class SystemHealthChecker
{
    public async Task<HealthCheckResult> PerformHealthCheckAsync()
    {
        var result = new HealthCheckResult();
        
        // 检查缓存一致性
        var cacheValidation = deviceCache.ValidateConsistency();
        result.CacheConsistency = cacheValidation.IsValid;
        
        // 检查选择状态
        result.SelectionValid = selectionStateManager.IsSelectionValid();
        
        // 检查数据库连接
        try
        {
            await configRepository.EnsureDefaultProfileAsync();
            result.DatabaseConnection = true;
        }
        catch
        {
            result.DatabaseConnection = false;
        }
        
        // 检查性能指标
        using (var perfAnalyzer = new PerformanceAnalyzer())
        {
            using (perfAnalyzer.MeasureOperation("HealthCheck_DeviceSelection"))
            {
                var device = deviceDataManager.Devices.FirstOrDefault();
                if (device != null)
                {
                    await selectionStateManager.SelectDeviceAsync(device.DeviceId);
                }
            }
            
            // 检查是否有慢操作
            result.PerformanceGood = true; // 根据实际测量结果设置
        }
        
        return result;
    }
}

public class HealthCheckResult
{
    public bool CacheConsistency { get; set; }
    public bool SelectionValid { get; set; }
    public bool DatabaseConnection { get; set; }
    public bool PerformanceGood { get; set; }
    
    public bool IsHealthy => CacheConsistency && SelectionValid && DatabaseConnection && PerformanceGood;
}
```

### 2. 自动恢复机制

```csharp
// 自动恢复服务
public class AutoRecoveryService
{
    private readonly Timer _recoveryTimer;
    
    public AutoRecoveryService()
    {
        _recoveryTimer = new Timer(PerformRecoveryCheck, null, 
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }
    
    private async void PerformRecoveryCheck(object? state)
    {
        try
        {
            var healthCheck = await systemHealthChecker.PerformHealthCheckAsync();
            
            if (!healthCheck.IsHealthy)
            {
                logger.LogWarning("系统健康检查失败，开始自动恢复");
                
                if (!healthCheck.CacheConsistency)
                {
                    await deviceDataManager.RefreshDataAsync(preserveSelection: true);
                }
                
                if (!healthCheck.SelectionValid)
                {
                    selectionStateManager.ClearSelection();
                    var firstDevice = deviceDataManager.Devices.FirstOrDefault();
                    if (firstDevice != null)
                    {
                        await selectionStateManager.SelectDeviceAsync(firstDevice.DeviceId);
                    }
                }
                
                logger.LogInformation("自动恢复完成");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "自动恢复过程中发生错误");
        }
    }
}
```

## 总结

本故障排除指南提供了全面的诊断和解决方案，涵盖了设备选择状态管理系统的常见问题。通过遵循这些步骤和使用提供的工具，开发人员可以快速识别和解决问题，确保系统的稳定运行。

记住始终：
1. 启用详细日志记录进行诊断
2. 使用性能分析工具监控系统健康
3. 实施预防性维护措施
4. 保持系统组件的一致性和同步

如果问题仍然存在，请查看详细的日志文件并联系开发团队获取进一步支持。