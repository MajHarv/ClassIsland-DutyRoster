namespace DutyRoster.Models.Rotation;

/// <summary>双周轮换，A / B 周各有独立组序列。</summary>
/// <remarks>
/// 把「A 周序列 + B 周序列」拼成长序列后按周取模；
/// SequenceLength 为 0 时退化为普通按周轮换。
/// </remarks>
public sealed class BiweeklyRotationPreset : IRotationPreset
{
    public string Kind => RotationKinds.Biweekly;

    public string DisplayName => "双周轮换";

    public string Description => "A 周与 B 周交替，两组序列轮换。";

    public int PeriodDays => 14;

    public int ResolveGroupIndex(RotationContext context, DateTime date)
    {
        if (context.GroupCount <= 0 || context.IsSkipped(date)) return -1;

        var weekIndex = GetWeekIndex(context, date);
        if (weekIndex < 0) return -1;

        var half = context.SequenceLength > 0 ? context.SequenceLength : context.GroupCount;
        var weekInBiweek = weekIndex % 2;
        var slot = (weekIndex / 2) % half;
        var index = weekInBiweek * half + slot;

        var auto = index % context.GroupCount;
        return context.ApplyManualOffset(auto);
    }

    public int ResolveDayInPeriod(RotationContext context, DateTime date)
    {
        if (context.IsSkipped(date)) return 0;
        var weekIndex = GetWeekIndex(context, date);
        if (weekIndex < 0) return 0;

        var weekStart = GetWeekStart(context.StartDate).AddDays(weekIndex * 7);
        return weekIndex % 2 * 7 + (date.Date - weekStart).Days + 1;
    }

    public string DescribeStatus(RotationContext context, DateTime date)
    {
        if (ResolveGroupIndex(context, date) < 0) return "今日不排值日";

        var weekIndex = GetWeekIndex(context, date);
        var weekStart = GetWeekStart(context.StartDate).AddDays(weekIndex * 7);
        var weekEnd = weekStart.AddDays(6);
        var phase = weekIndex % 2 == 0 ? "A 周" : "B 周";
        var round = weekIndex / 14 + 1;
        return $"{phase} · 第 {round} 个双周（{weekStart:M/d} – {weekEnd:M/d}）";
    }

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
