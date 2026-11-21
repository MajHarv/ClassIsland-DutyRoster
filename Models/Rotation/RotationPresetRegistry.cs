namespace DutyRoster.Models.Rotation;

/// <summary>轮换方式标识符，会写入配置文件，不可随意变更。</summary>
public static class RotationKinds
{
    public const string Weekly = "weekly";
    public const string Daily = "daily";
    public const string Biweekly = "biweekly";
    public const string Default = Weekly;
}

/// <summary>
/// 轮换预设注册表。新增轮换方式只需实现 IRotationPreset 并在 _presets 登记，
/// 组件、提醒与设置页无需改动。
/// </summary>
public static class RotationPresetRegistry
{
    private static readonly List<IRotationPreset> _presets =
    [
        new WeeklyRotationPreset(),
        new DailyRotationPreset(),
        new BiweeklyRotationPreset()
    ];

    public static IReadOnlyList<IRotationPreset> All => _presets;

    /// <summary>按标识符查找，未知标识回退到默认，避免旧配置导致崩溃。</summary>
    public static IRotationPreset Resolve(string? kind)
    {
        if (!string.IsNullOrWhiteSpace(kind))
        {
            foreach (var p in _presets)
            {
                if (string.Equals(p.Kind, kind, StringComparison.OrdinalIgnoreCase))
                    return p;
            }
        }
        return _presets[0];
    }
}
