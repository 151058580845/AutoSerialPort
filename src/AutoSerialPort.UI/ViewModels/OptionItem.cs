namespace AutoSerialPort.UI.ViewModels;

/// <summary>
/// 下拉选项模型。
/// </summary>
public class OptionItem
{
    /// <summary>
    /// 创建选项。
    /// </summary>
    /// <param name="value">选项值。</param>
    /// <param name="displayName">显示名称。</param>
    public OptionItem(string value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }

    /// <summary>
    /// 选项值。
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// 显示名称。
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// 判断两个选项项是否相等（基于Value比较）
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is OptionItem other)
        {
            return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    /// <summary>
    /// 获取哈希码
    /// </summary>
    public override int GetHashCode()
    {
        return Value?.ToLowerInvariant().GetHashCode() ?? 0;
    }
}
