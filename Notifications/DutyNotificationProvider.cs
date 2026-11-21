using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Notification;
using DutyRoster.Models;
using DutyRoster.Services;

namespace DutyRoster.Notifications;

/// <summary>
/// 值日提醒提供方，在每个值日时段开始时提醒。
/// 提醒的开关、音量、时长等由 ClassIsland 原生提醒设置统一管理。
/// </summary>
[NotificationProviderInfo(
    "C3D4E5F6-A7B8-4901-BCDE-F01234567890",
    "值日生提醒",
    "\uE7C4",
    "在值日时段开始时提醒当前值日组")]
public class DutyNotificationProvider : NotificationProviderBase<DutyNotificationSettings>
{
    private readonly DispatcherTimer _timer;
    private readonly HashSet<string> _firedKeys = new();
    private DateOnly _firedDate = DateOnly.MinValue;
    private bool _isFirstPoll = true;

    public DutyNotificationProvider()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _timer.Tick += (_, _) => Poll();

        Plugin.RegisterNotificationProvider(this);

        Dispatcher.UIThread.Post(() => _timer.Start(), DispatcherPriority.Background);
    }

    /// <summary>轮询而非精确调度：对系统睡眠/时间跳变更健壮，30 秒误差对值日提醒可接受。</summary>
    private void Poll()
    {
        try
        {
            var config = Plugin.Config;
            if (!config.IsEnabled || !config.EnableNotification) return;
            if (Settings is not null && !Settings.IsEnabled) return;

            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);

            // 首次轮询或跨天时，预标记「启动前就应触发的时段」，避免把当天已过点的时段补发出来
            if (_isFirstPoll || today != _firedDate)
            {
                PreMarkAlreadyFired(config, now);
                _firedDate = today;
                _isFirstPoll = false;
            }

            var group = config.ResolveGroup(now);
            if (group is null) return;

            var types = config.ActiveTypes;
            if (types.Count == 0) return;

            var lead = TimeSpan.FromSeconds(Math.Max(0, Settings?.LeadSeconds ?? 0));

            foreach (var schedule in config.Schedules)
            {
                if (!schedule.IsEnabled || !schedule.AllowReminder) continue;

                var fireAt = now.Date + schedule.Start - lead;
                var key = $"{today:yyyyMMdd}_{schedule.Name}_{schedule.Start}";

                // 到点即触发（当天仅一次）。不设 30 秒上限窗口，
                // 避免 tick 恰好跨过窗口导致漏发。
                if (now < fireAt) continue;
                if (!_firedKeys.Add(key)) continue;

                Fire(group, schedule, types, now);
            }
        }
        catch
        {
            // 提醒失败不影响主程序
        }
    }

    /// <summary>
    /// 把「今天已经过了触发点、但还没被记录」的时段预标记为已触发，
    /// 避免 ClassIsland 启动（或跨天）时把已过点的时段一次性补发。
    /// </summary>
    private void PreMarkAlreadyFired(DutyConfig config, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);
        var lead = TimeSpan.FromSeconds(Math.Max(0, Settings?.LeadSeconds ?? 0));

        foreach (var schedule in config.Schedules)
        {
            if (!schedule.IsEnabled || !schedule.AllowReminder) continue;

            var fireAt = now.Date + schedule.Start - lead;
            if (now >= fireAt)
            {
                var key = $"{today:yyyyMMdd}_{schedule.Name}_{schedule.Start}";
                _firedKeys.Add(key);
            }
        }
    }

    private void Fire(DutyGroup group, DutySchedule schedule, IReadOnlyList<DutyType> types, DateTime now)
    {
        var config = Plugin.Config;
        var settings = Settings;

        var maskText = DutyFormatter.RenderTemplate(
            settings?.MaskTemplate, config, group, schedule, now, types);
        if (string.IsNullOrWhiteSpace(maskText))
            maskText = $"{group.Name} 值日";

        var contentText = DutyFormatter.RenderTemplate(
            settings?.ContentTemplate ?? "{typesLines}", config, group, schedule, now, types);
        if (string.IsNullOrWhiteSpace(contentText))
            contentText = DutyFormatter.FormatGroupMultiline(group, types);

        var mask = NotificationContent.CreateTwoIconsMask(maskText, factory: x =>
        {
            x.Duration = TimeSpan.FromSeconds(3);
        });

        var overlay = NotificationContent.CreateSimpleTextContent(contentText, factory: x =>
        {
            x.Duration = TimeSpan.FromSeconds(6);
            if (settings?.IsSpeechEnabled == true)
            {
                x.IsSpeechEnabled = true;
                x.SpeechContent = $"值日提醒：{group.Name}。{DutyFormatter.FormatForSpeech(group, types)}";
            }
        });

        ShowNotification(new NotificationRequest
        {
            MaskContent = mask,
            OverlayContent = overlay
        });
    }
}
