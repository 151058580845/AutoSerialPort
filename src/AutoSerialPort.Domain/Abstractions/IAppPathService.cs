namespace AutoSerialPort.Domain.Abstractions;

/// <summary>
/// 应用路径服务，统一提供数据与日志目录。
/// </summary>
public interface IAppPathService
{
    /// <summary>
    /// 应用数据目录。
    /// </summary>
    string AppDataDirectory { get; }

    /// <summary>
    /// 数据库文件路径。
    /// </summary>
    string DatabasePath { get; }

    /// <summary>
    /// 日志目录。
    /// </summary>
    string LogDirectory { get; }
}
