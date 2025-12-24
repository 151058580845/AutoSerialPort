namespace AutoSerialPort.Domain.Models;

/// <summary>
/// 串口描述符
/// 包含串口设备的详细识别信息
/// </summary>
public class SerialPortDescriptor
{
    /// <summary>
    /// 串口名称（如COM1、COM2等）
    /// </summary>
    public string PortName { get; set; } = string.Empty;

    /// <summary>
    /// 设备显示名称
    /// 用于在用户界面中显示的友好名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 即插即用设备ID
    /// Windows系统中用于唯一标识设备的ID
    /// </summary>
    public string? PnpDeviceId { get; set; }

    /// <summary>
    /// 供应商ID和产品ID组合
    /// 格式通常为"VID_xxxx&PID_xxxx"
    /// </summary>
    public string? VidPid { get; set; }

    /// <summary>
    /// Linux系统中的设备路径
    /// 通常位于/dev/serial/by-id/目录下
    /// </summary>
    public string? ByIdPath { get; set; }
}
