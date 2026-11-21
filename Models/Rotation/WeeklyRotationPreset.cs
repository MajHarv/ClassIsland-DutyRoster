namespace DutyRoster.Models.Rotation;

/// <summary>按周轮换，每周换一组。</summary>
public sealed class WeeklyRotationPreset : IRotationPreset
{
    public string Kind => RotationKinds.Weekly;

    public string DisplayName => "按周轮换";

    public string Description => "每周轮到下一组，6 个组即为 6 周一循环。";

    public int PeriodDays => 7;

    public int ResolveGroupIndex(RotationContext context, DateTime date)
    {
        if (context.GroupCount <= 0 || context.IsSkipped(date)) return -1;

        var weekIndex = GetWeekIndex(context, date);
        if (weekIndex < 0) return -1;

        var group = weekIndex % context.GroupCount;
        if (group < 0) group += context.GroupCount;

        return context.ApplyManualOffset(group);
    }

    public int ResolveDayInPeriod(RotationContext context, DateTime date)
    {
        if (context.IsSkipped(date)) return 0;
        var weekIndex = GetWeekIndex(context, date);
        if (weekIndex < 0) return 0;

        var weekStart = GetWeekStart(context.StartDate).AddDays(weekIndex * 7);
        return (date.Date - weekStart).Days + 1;
    }

    public string DescribeStatus(RotationContext context, DateTime date)
    {
        if (ResolveGroupIndex(context, date) < 0) return "今日不排值日";

        var weekIndex = GetWeekIndex(context, date);
        var weekStart = GetWeekStart(context.StartDate).AddDays(weekIndex * 7);
        var weekEnd = weekStart.AddDays(6);
        return $"第 {weekIndex + 1} 周（{weekStart:M/d} – {weekEnd:M/d}）";
    }

    /// <summary>相对起始周的周序号，起始周为 0。</summary>
    private static int GetWeekIndex(RotationContext context, DateTime date)
    {
        var startWeek = GetWeekStart(context.StartDate);
        var targetWeek = GetWeekStart(date);
        return (int)((targetWeek - startWeek).TotalDays / 7);
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-diff);
    }
}
