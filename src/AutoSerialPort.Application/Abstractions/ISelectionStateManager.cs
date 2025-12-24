using System;
using System.Threading;
using System.Threading.Tasks;
using AutoSerialPort.Application.Models;

namespace AutoSerialPort.Application.Abstractions;

/// <summary>
/// 设备选择状态管理器接口
/// 负责管理设备选择状态，提供防抖和冲突解决功能
/// </summary>
public interface ISelectionStateManager : IDisposable
{
    /// <summary>
    /// 当前选中的设备
    /// </summary>
    IDeviceProfileViewModel? SelectedDevice { get; }

    /// <summary>
    /// 设备选择状态变更事件
    /// </summary>
    event EventHandler<DeviceSelectionChangedEventArgs> SelectionChanged;

    /// <summary>
    /// 根据设备ID异步选择设备
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>选择是否成功</returns>
    Task<bool> SelectDeviceAsync(long deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据显示名称异步选择设备
    /// </summary>
    /// <param name="displayName">显示名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>选择是否成功</returns>
    Task<bool> SelectDeviceAsync(string displayName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 直接选择设备对象（用于UI绑定同步）
    /// </summary>
    /// <param name="device">设备视图模型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>选择是否成功</returns>
    Task<bool> SelectDeviceAsync(IDeviceProfileViewModel device, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清除当前选择
    /// </summary>
    void ClearSelection();

    /// <summary>
    /// 验证当前选择状态是否有效
    /// </summary>
    /// <returns>选择状态是否有效</returns>
    bool IsSelectionValid();
}