using SqlSugar;

namespace AutoSerialPort.Domain.Entities;

/// <summary>
/// 应用程序全局设置实体
/// 存储应用程序的全局配置信息，如主题、自动启动、窗口状态等
/// 对应数据库表：app_settings
/// </summary>
[SugarTable("app_settings")]
public class AppSettings
{
    /// <summary>
    /// 主键ID，自增长
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDataType = "INTEGER")]
    public long Id { get; set; }

    /// <summary>
    /// 应用程序主题
    /// 可选值：Light（浅色主题）、Dark（深色主题）
    /// 默认值：Light
    /// </summary>
    [SugarColumn(Length = 64)]
    public string Theme { get; set; } = "Light";

    /// <summary>
    /// 是否开机自动启动
    /// </summary>
    public bool AutoStart { get; set; }

    /// <summary>
    /// 上次使用的配置文件ID
    /// 用于记录用户最后一次使用的配置方案，下次启动时自动加载
    /// 默认值：1（默认配置文件）
    /// </summary>
    public long LastProfileId { get; set; } = 1;

    /// <summary>
    /// 窗口状态JSON字符串
    /// 存储窗口的位置、大小、最大化状态等信息
    /// 用于下次启动时恢复窗口状态
    /// </summary>
    [SugarColumn(Length = 256, IsNullable = true)]
    public string? WindowStateJson { get; set; }
}
