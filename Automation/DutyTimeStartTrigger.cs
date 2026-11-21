using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using DutyRoster.Models;

namespace DutyRoster.Automation;

/// <summary>自动化触发器：值日时段开始时触发。</summary>
[TriggerInfo("dutylist.roster.onDutyTimeStart", "值日时段开始时", "\uE7C4")]
public class DutyTimeStartTrigger : TriggerBase<DutyTimeStartTriggerSettings>
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(30) };
    private readonly HashSet<string> _firedKeys = new();
    private DateOnly _firedDate = DateOnly.MinValue;
    private bool _isFirstTick = true;

    public override void Loaded()
    {
        _timer.Tick += OnTick;
        Dispatcher.UIThread.Post(() => _timer.Start(), DispatcherPriority.Background);
    }

    /// <summary>必须在此停止计时器并解绑，否则工作流删除后仍会触发并造成泄漏。</summary>
    public override void UnLoaded()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
        _firedKeys.Clear();
        _isFirstTick = true;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        try
        {
            var config = Plugin.Config;
            if (!config.IsEnabled) return;

            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);

            // 首次或跨天时，预标记「加载前就应触发的时段」，避免补发
            if (_isFirstTick || today != _firedDate)
            {
                PreMarkAlreadyFired(config, now);
                _firedDate = today;
                _isFirstTick = false;
            }

            if (Settings.RequireDutyToday && config.ResolveGroup(now) is null) return;

            var offset = TimeSpan.FromSeconds(Settings.OffsetSeconds);
            var wantedName = Settings.ScheduleName?.Trim() ?? "";

            foreach (var schedule in config.Schedules)
            {
                if (!schedule.IsEnabled) continue;
                if (wantedName.Length > 0 &&
                    !string.Equals(schedule.Name, wantedName, StringComparison.Ordinal))
                    continue;

                var fireAt = now.Date + schedule.Start + offset;
                // 到点即触发（当天仅一次），不设 30 秒上限窗口以免漏发
                if (now < fireAt) continue;

                var key = $"{today:yyyyMMdd}_{schedule.Name}";
                if (!_firedKeys.Add(key)) continue;

                Dispatcher.UIThread.Post(Trigger);
            }
        }
        catch
        {
            // 触发器异常不应导致自动化服务崩溃
        }
    }

    /// <summary>预标记「加载前已过触发点」的时段，避免启动时补发。</summary>
    private void PreMarkAlreadyFired(DutyConfig config, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);
        var offset = TimeSpan.FromSeconds(Settings.OffsetSeconds);
        var wantedName = Settings.ScheduleName?.Trim() ?? "";

        foreach (var schedule in config.Schedules)
        {
            if (!schedule.IsEnabled) continue;
            if (wantedName.Length > 0 &&
                !string.Equals(schedule.Name, wantedName, StringComparison.Ordinal))
                continue;

            var fireAt = now.Date + schedule.Start + offset;
            if (now >= fireAt)
            {
                var key = $"{today:yyyyMMdd}_{schedule.Name}";
                _firedKeys.Add(key);
            }
        }
    }
}
