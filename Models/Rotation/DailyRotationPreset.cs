namespace DutyRoster.Models.Rotation;

/// <summary>按日轮换，每天换一组。</summary>
public sealed class DailyRotationPreset : IRotationPreset
{
    public string Kind => RotationKinds.Daily;

    public string DisplayName => "按日轮换";

    public string Description => "每天都轮到下一组，6 个组即为 6 天一循环。";

    public int PeriodDays => 1;

    public int ResolveGroupIndex(RotationContext context, DateTime date)
    {
        if (context.GroupCount <= 0 || context.IsSkipped(date)) return -1;
        if (date.Date < context.StartDate.Date) return -1;

        var effectiveDays = context.CountEffectiveDays(date);
        if (effectiveDays <= 0) return -1;

        var auto = (effectiveDays - 1) % context.GroupCount;
        return context.ApplyManualOffset(auto);
    }

    public int ResolveDayInPeriod(RotationContext context, DateTime date)
    {
        if (context.IsSkipped(date) || date.Date < context.StartDate.Date) return 0;
        return 1;
    }

    public string DescribeStatus(RotationContext context, DateTime date)
    {
        if (ResolveGroupIndex(context, date) < 0) return "今日不排值日";

        var effectiveDays = context.CountEffectiveDays(date);
        var cycle = context.GroupCount;
        var round = (effectiveDays - 1) / cycle + 1;
        var dayInCycle = (effectiveDays - 1) % cycle + 1;
        return $"第 {round} 轮 · 第 {dayInCycle}/{cycle} 天";
    }
}
