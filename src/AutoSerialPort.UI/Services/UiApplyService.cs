using System;
using System.Threading.Tasks;

namespace AutoSerialPort.UI.Services;

/// <summary>
/// UI 配置应用协调器，用于跨视图模型触发保存/应用操作。
/// </summary>
public interface IUiApplyService
{
    /// <summary>
    /// 注册执行保存/应用的回调。
    /// </summary>
    /// <param name="applyAsync">保存/应用回调。</param>
    void Register(Func<Task> applyAsync);

    /// <summary>
    /// 触发保存/应用。
    /// </summary>
    Task ApplyAsync();
}

/// <summary>
/// UI 配置应用协调器实现。
/// </summary>
public class UiApplyService : IUiApplyService
{
    private Func<Task>? _applyAsync;

    /// <summary>
    /// 注册执行保存/应用的回调。
    /// </summary>
    /// <param name="applyAsync">保存/应用回调。</param>
    public void Register(Func<Task> applyAsync)
    {
        _applyAsync = applyAsync;
    }

    /// <summary>
    /// 触发保存/应用。
    /// </summary>
    public Task ApplyAsync()
    {
        var apply = _applyAsync;
        return apply == null ? Task.CompletedTask : apply();
    }
}
