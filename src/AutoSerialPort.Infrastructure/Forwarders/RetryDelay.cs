using System;

namespace AutoSerialPort.Infrastructure.Forwarders;

/// <summary>
/// 重试延迟策略，使用指数退避。
/// </summary>
public class RetryDelay
{
    private readonly TimeSpan _min;
    private readonly TimeSpan _max;
    private TimeSpan _current;

    /// <summary>
    /// 创建重试延迟策略。
    /// </summary>
    /// <param name="min">最小延迟。</param>
    /// <param name="max">最大延迟。</param>
    public RetryDelay(TimeSpan min, TimeSpan max)
    {
        _min = min;
        _max = max;
        _current = min;
    }

    /// <summary>
    /// 获取下一次重试延迟。
    /// </summary>
    public TimeSpan Next()
    {
        var delay = _current;
        var nextMs = Math.Min(_current.TotalMilliseconds * 2, _max.TotalMilliseconds);
        _current = TimeSpan.FromMilliseconds(nextMs);
        return delay;
    }

    /// <summary>
    /// 重置为最小延迟。
    /// </summary>
    public void Reset()
    {
        _current = _min;
    }
}
