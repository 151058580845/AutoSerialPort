using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using AutoSerialPort.Application.Models;

namespace AutoSerialPort.Application.Abstractions;

/// <summary>
/// 设备数据管理器接口
/// 负责设备数据的加载、缓存和预加载操作
/// </summary>
public interface IDeviceDataManager
{
    /// <summary>
    /// 设备集合
    /// </summary>
    ObservableCollection<IDeviceProfileViewModel> Devices { get; }

    /// <summary>
    /// 是否正在加载数据
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// 数据加载状态变更事件
    /// </summary>
    event EventHandler<DataLoadingEventArgs> LoadingStateChanged;

    /// <summary>
    /// 预加载数据
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>预加载任务</returns>
    Task PreloadDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新数据
    /// </summary>
    /// <param name="preserveSelection">是否保持当前选择</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>刷新任务</returns>
    Task RefreshDataAsync(bool preserveSelection = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据设备ID获取设备
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <returns>设备视图模型，如果未找到则返回null</returns>
    Task<IDeviceProfileViewModel?> GetDeviceAsync(long deviceId);

    /// <summary>
    /// 根据显示名称获取设备
    /// </summary>
    /// <param name="displayName">显示名称</param>
    /// <returns>设备视图模型，如果未找到则返回null</returns>
    Task<IDeviceProfileViewModel?> GetDeviceAsync(string displayName);
    
    /// <summary>
    /// 智能刷新数据（用于窗口重新打开场景的优化）
    /// </summary>
    /// <param name="forceRefresh">是否强制刷新，忽略缓存有效性</param>
    /// <param name="preserveSelection">是否保持选择状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SmartRefreshAsync(bool forceRefresh = false, bool preserveSelection = true, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取缓存是否有效（用于优化窗口重新打开场景）
    /// </summary>
    bool IsCacheValid { get; }
    
    /// <summary>
    /// 获取是否需要后台刷新
    /// </summary>
    bool ShouldBackgroundRefresh { get; }
    
    /// <summary>
    /// 启用或禁用后台刷新
    /// </summary>
    /// <param name="enabled">是否启用</param>
    void SetBackgroundRefreshEnabled(bool enabled);
}