using DutyRoster.Models;

namespace DutyRoster.Services;

/// <summary>把值日配置格式化为界面 / 提醒文本。</summary>
public static class DutyFormatter
{
    public static string FormatAssignment(DutyGroup group, DutyType type)
    {
        var people = GetPeople(group, type.Name);
        var label = string.IsNullOrWhiteSpace(type.Icon) ? type.Name : $"{type.Icon} {type.Name}";
        return people.Count == 0 ? $"{label}：（未安排）" : $"{label}：{string.Join("、", people)}";
    }

    public static string FormatGroup(DutyGroup group, IEnumerable<DutyType> types)
    {
        var parts = types.Select(t => FormatAssignment(group, t)).ToList();
        return parts.Count == 0 ? "（暂未配置值日类型）" : string.Join("  |  ", parts);
    }

    public static string FormatGroupMultiline(DutyGroup group, IEnumerable<DutyType> types)
        => string.Join(Environment.NewLine, types.Select(t => FormatAssignment(group, t)));

    /// <summary>语音朗读用纯文本，去掉分隔符便于 TTS 朗读。</summary>
    public static string FormatForSpeech(DutyGroup group, IEnumerable<DutyType> types)
    {
        var parts = new List<string>();
        foreach (var type in types)
        {
            var people = GetPeople(group, type.Name);
            if (people.Count == 0) continue;
            parts.Add($"{type.Name}：{string.Join("、", people)}");
        }
        return parts.Count == 0 ? "" : string.Join("；", parts);
    }

    /// <summary>
    /// 渲染模板。占位符：{group} 组名、{types} 单行、{typesLines} 多行、
    /// {schedule} 时段名、{date} 日期、{table} 表名、
    /// {type} 首个值日类型名、{name} 首个值日类型的同学姓名（顿号连接）。
    /// </summary>
    public static string RenderTemplate(
        string? template,
        DutyConfig config,
        DutyGroup? group,
        DutySchedule? schedule,
        DateTime date,
        IEnumerable<DutyType>? types = null)
    {
        if (string.IsNullOrEmpty(template)) return "";

        var useTypes = (types ?? config.ActiveTypes).ToList();
        var result = template
            .Replace("{group}", group?.Name ?? "无")
            .Replace("{date}", date.ToString("yyyy/M/d"))
            .Replace("{schedule}", schedule?.Name ?? "")
            .Replace("{table}", config.Name);

        // {type} / {name}：取第一个值日类型（Action 指定单类别时的核心占位符）
        var firstType = useTypes.FirstOrDefault();
        result = result
            .Replace("{type}", firstType?.Name ?? "")
            .Replace("{name}", group != null && firstType != null
                ? string.Join("、", GetPeople(group, firstType.Name))
                : "");

        result = result.Replace("{typesLines}", group is null
            ? "今日不排值日"
            : FormatGroupMultiline(group, useTypes));

        result = result.Replace("{types}", group is null
            ? "今日不排值日"
            : FormatGroup(group, useTypes));

        return result;
    }

    public static List<string> GetPeople(DutyGroup group, string typeName)
    {
        var assignment = group.Assignments.FirstOrDefault(
            a => string.Equals(a.TypeName, typeName, StringComparison.Ordinal));
        return assignment?.People.Where(p => !string.IsNullOrWhiteSpace(p)).ToList() ?? new List<string>();
    }
}
