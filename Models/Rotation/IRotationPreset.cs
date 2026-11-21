namespace DutyRoster.Models.Rotation;

/// <summary>轮换预设接口。值日表本身是静态二维表，由轮换预设决定今天轮到哪一组。</summary>
public interface IRotationPreset
{
    /// <summary>稳定标识符，会写入配置。</summary>
    string Kind { get; }

    string DisplayName { get; }

    string Description { get; }

    /// <summary>轮换周期天数。</summary>
    int PeriodDays { get; }

    /// <summary>返回组索引，-1 表示当天不排值日。</summary>
    int ResolveGroupIndex(RotationContext context, DateTime date);

    /// <summary>返回当前周期内的第几天（从 1 开始），0 表示不参与轮换。</summary>
    int ResolveDayInPeriod(RotationContext context, DateTime date);

    string DescribeStatus(RotationContext context, DateTime date);
}

/// <summary>轮换计算所需的全部外部数据，保持预设实现无状态。</summary>
public sealed class RotationContext
{
    public DateTime StartDate { get; init; } = DateTime.Today;

    public int GroupCount { get; init; } = 1;

    public IReadOnlySet<DayOfWeek> SkipDays { get; init; } = new HashSet<DayOfWeek>();

    public IReadOnlyCollection<DateTime> ExtraSkipDates { get; init; } = Array.Empty<DateTime>();

    /// <summary>序列长度，双周轮换用于表示每半周期的组数。</summary>
    public int SequenceLength { get; init; }

    /// <summary>手动组偏移，加到自动计算出的组索引上（取模后归一化）。</summary>
    public int ManualGroupOffset { get; init; }

    /// <summary>对自动算出的组索引应用手动偏移，返回归一化到 [0, groupCount) 的结果。</summary>
    public int ApplyManualOffset(int autoIndex)
    {
        if (autoIndex < 0 || ManualGroupOffset == 0) return autoIndex;
        var n = GroupCount;
        return ((autoIndex + ManualGroupOffset) % n + n) % n;
    }

    public bool IsSkipped(DateTime date)
    {
        if (SkipDays.Contains(date.DayOfWeek)) return true;
        var d = date.Date;
        foreach (var skip in ExtraSkipDates)
        {
            if (skip.Date == d) return true;
        }
        return false;
    }

    /// <summary>从起始日到指定日期之间计入轮换的天数（含首尾，跳过日不计）。</summary>
    public int CountEffectiveDays(DateTime date)
    {
        if (date.Date < StartDate.Date) return 0;
        var count = 0;
        for (var d = StartDate.Date; d <= date.Date; d = d.AddDays(1))
        {
            if (!IsSkipped(d)) count++;
        }
        return count;
    }

    public static RotationContext Create(RotationSettings settings, int groupCount)
    {
        return new RotationContext
        {
            StartDate = settings.StartDate,
            GroupCount = Math.Max(1, groupCount),
            SkipDays = new HashSet<DayOfWeek>(settings.SkipDays),
            ExtraSkipDates = settings.ExtraSkipDates,
            SequenceLength = settings.SequenceLength,
            ManualGroupOffset = settings.ManualGroupOffset
        };
    }
}
