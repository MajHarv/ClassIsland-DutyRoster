namespace DutyRoster.Automation;

/// <summary>
/// 值日时段开始触发器的设置。
/// </summary>
public class DutyTimeStartTriggerSettings
{
    /// <summary>
    /// 要监听的时段名称。留空表示任意值日时段。
    /// </summary>
    public string ScheduleName { get; set; } = "";

    /// <summary>
    /// 提前多少秒触发。0 表示到点触发，负数表示延后。
    /// </summary>
    public int OffsetSeconds { get; set; }

    /// <summary>
    /// 是否只在「今天有值日安排」时触发。
    /// </summary>
    public bool RequireDutyToday { get; set; } = true;
}
