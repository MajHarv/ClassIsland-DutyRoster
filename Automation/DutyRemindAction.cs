using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Notification;
using DutyRoster.Models;
using DutyRoster.Services;

namespace DutyRoster.Automation;

/// <summary>
/// 自动化行动：提醒当前组某个值日类别的同学去值日。
/// 可在 ClassIsland 自动化中自由调用。
/// </summary>
[ActionInfo(
    "dutylist.roster.remindDuty",
    "值日提醒",
    "\uE7C4",
    addDefaultToMenu: true,
    defaultGroupToMenu: "值日")]
public class DutyRemindAction : ActionBase<DutyRemindActionSettings>
{
    protected override Task OnInvoke()
    {
        // 先调用基类，管理行动项运行生命周期
        base.OnInvoke();

        var config = Plugin.Config;
        if (!config.IsEnabled) return Task.CompletedTask;

        var groups = config.ActiveGroups;
        if (groups.Count == 0) return Task.CompletedTask;

        var group = ResolveTargetGroup(config, groups);
        if (group is null) return Task.CompletedTask;

        var types = ResolveTargetTypes(config);
        if (types.Count == 0) return Task.CompletedTask;

        ShowReminder(config, group, types);
        return Task.CompletedTask;
    }

    private DutyGroup? ResolveTargetGroup(DutyConfig config, IReadOnlyList<DutyGroup> groups)
    {
        if (Settings.UseCurrentGroup)
        {
            return config.ResolveGroup(DateTime.Now);
        }

        var idx = Settings.GroupIndex - 1;
        return idx >= 0 && idx < groups.Count ? groups[idx] : null;
    }

    private List<DutyType> ResolveTargetTypes(DutyConfig config)
    {
        var all = config.ActiveTypes;
        if (string.IsNullOrWhiteSpace(Settings.DutyTypeName)) return all.ToList();

        var matched = all.Where(t =>
            string.Equals(t.Name, Settings.DutyTypeName, StringComparison.Ordinal)).ToList();
        return matched.Count > 0 ? matched : all.ToList();
    }

    private void ShowReminder(DutyConfig config, DutyGroup group, List<DutyType> types)
    {
        var maskText = DutyFormatter.RenderTemplate(
            Settings.MaskTemplate, config, group, null, DateTime.Now, types);
        if (string.IsNullOrWhiteSpace(maskText)) maskText = $"{group.Name} 值日";

        var contentText = DutyFormatter.RenderTemplate(
            Settings.ContentTemplate ?? "{types}", config, group, null, DateTime.Now, types);
        if (string.IsNullOrWhiteSpace(contentText))
            contentText = DutyFormatter.FormatGroup(group, types);

        Dispatcher.UIThread.Post(() =>
        {
            var mask = NotificationContent.CreateTwoIconsMask(maskText, factory: x =>
            {
                x.Duration = TimeSpan.FromSeconds(Settings.MaskDurationSeconds);
            });

            var overlay = NotificationContent.CreateSimpleTextContent(contentText, factory: x =>
            {
                x.Duration = TimeSpan.FromSeconds(Settings.ContentDurationSeconds);
                if (Settings.IsSpeechEnabled)
                {
                    x.IsSpeechEnabled = true;
                    x.SpeechContent = $"值日提醒：{group.Name}。{DutyFormatter.FormatForSpeech(group, types)}";
                }
            });

            Plugin.ShowDutyNotification(mask, overlay);
        });
    }
}
