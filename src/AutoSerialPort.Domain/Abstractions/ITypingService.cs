using System.Threading;
using System.Threading.Tasks;

namespace AutoSerialPort.Domain.Abstractions;

/// <summary>
/// 模拟输入服务，负责跨平台模拟键盘输入。
/// </summary>
public interface ITypingService
{
    /// <summary>
    /// 是否可用。
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// 尝试模拟输入文本。
    /// </summary>
    /// <param name="text">待输入文本。</param>
    /// <param name="ct">取消令牌。</param>
    Task<bool> TryTypeAsync(string text, CancellationToken ct);

    /// <summary>
    /// 获取不可用原因。
    /// </summary>
    string? GetUnavailableReason();
}
