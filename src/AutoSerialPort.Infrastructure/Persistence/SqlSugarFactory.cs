using AutoSerialPort.Domain.Abstractions;
using SqlSugar;

namespace AutoSerialPort.Infrastructure.Persistence;

/// <summary>
/// SQLSugar 工厂，用于创建数据库上下文。
/// </summary>
public static class SqlSugarFactory
{
    /// <summary>
    /// 创建 SQLSugar 上下文。
    /// </summary>
    /// <param name="pathService">路径服务。</param>
    public static SqlSugarScope Create(IAppPathService pathService)
    {
        // SQLite 连接配置
        var config = new ConnectionConfig
        {
            ConnectionString = $"Data Source={pathService.DatabasePath}",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        };

        return new SqlSugarScope(config);
    }
}
