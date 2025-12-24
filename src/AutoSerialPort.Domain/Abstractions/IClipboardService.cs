using System.Threading.Tasks;

namespace AutoSerialPort.Domain.Abstractions;

/// <summary>
/// 剪贴板服务，提供跨平台文本写入能力。
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// 写入剪贴板文本。
    /// </summary>
    /// <param name="text">要写入的内容。</param>
    Task SetTextAsync(string text);
}
