using System.Collections.Generic;

namespace AutoSerialPort.Application.Abstractions;

/// <summary>
/// 设备缓存接口
/// 提供线程安全的设备数据缓存和高效查找功能
/// </summary>
public interface IDeviceCache
{
    /// <summary>
    /// 获取所有缓存的设备
    /// </summary>
    /// <returns>只读设备列表</returns>
    IReadOnlyList<IDeviceProfileViewModel> GetAll();

    /// <summary>
    /// 根据ID获取设备
    /// </summary>
    /// <param name="id">设备ID</param>
    /// <returns>设备视图模型，如果未找到则返回null</returns>
    IDeviceProfileViewModel? GetById(long id);

    /// <summary>
    /// 根据显示名称获取设备
    /// </summary>
    /// <param name="displayName">显示名称</param>
    /// <returns>设备视图模型，如果未找到则返回null</returns>
    IDeviceProfileViewModel? GetByDisplayName(string displayName);

    /// <summary>
    /// 更新缓存
    /// </summary>
    /// <param name="devices">设备集合</param>
    void UpdateCache(IEnumerable<IDeviceProfileViewModel> devices);

    /// <summary>
    /// 添加单个设备到缓存
    /// </summary>
    /// <param name="device">设备视图模型</param>
    /// <returns>是否添加成功</returns>
    bool Add(IDeviceProfileViewModel device);

    /// <summary>
    /// 从缓存中移除设备
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <returns>是否���除成功</returns>
    bool Remove(long deviceId);

    /// <summary>
    /// 清空缓存
    /// </summary>
    void Clear();

    /// <summary>
    /// 缓存是否为空
    /// </summary>
    bool IsEmpty { get; }
}