using System;

namespace AutoSerialPort.Infrastructure.Forwarders;

/// <summary>
/// 简单的日志节流器，避免短时间内重复输出。
/// </summary>
public class ErrorThrottle
{
    private readonly TimeSpan _interval;
    private DateTimeOffset _lastLog = DateTimeOffset.MinValue;

    /// <summary>
    /// 创建节流器。
    /// </summary>
    /// <param name="interval">最小输出间隔。</param>
    public ErrorThrottle(TimeSpan interval)
    {
        _interval = interval;
    }

    /// <summary>
    /// 判断是否允许输出日志。
    /// </summary>
    public bool ShouldLog()
    {
        var now = DateTimeOffset.Now;
        if (now - _lastLog >= _interval)
        {
            _lastLog = now;
            return true;
        }

        return false;
    }
}
