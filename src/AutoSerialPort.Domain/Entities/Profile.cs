using SqlSugar;

namespace AutoSerialPort.Domain.Entities;

/// <summary>
/// 配置文件实体
/// 用于支持多套配置方案，用户可以创建多个配置文件来管理不同的设备配置组合
/// 其他配置实体（如 SerialDeviceConfig、ParserConfig 等）通过 ProfileId 关联到此配置文件
/// 对应数据库表：profiles
/// </summary>
[SugarTable("profiles")]
public class Profile
{
    /// <summary>
    /// 主键ID，自增长
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDataType = "INTEGER")]
    public long Id { get; set; }

    /// <summary>
    /// 配置文件名称
    /// 用于在界面上显示和区分不同的配置方案
    /// 默认值：Default
    /// </summary>
    [SugarColumn(Length = 64)]
    public string Name { get; set; } = "Default";
}
