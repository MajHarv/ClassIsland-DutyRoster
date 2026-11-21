using Avalonia;
using ClassIsland.Core.Abstractions.Controls;

namespace DutyRoster.Automation;

/// <summary>值日时段开始触发器的设置界面。</summary>
public partial class DutyTimeStartTriggerSettingsControl : TriggerSettingsControlBase<DutyTimeStartTriggerSettings>
{
    private const string AnyScheduleLabel = "任意时段";
    private bool _isLoaded;

    public DutyTimeStartTriggerSettingsControl()
    {
        InitializeComponent();

        var items = new List<string> { AnyScheduleLabel };
        items.AddRange(Plugin.Config.Schedules.Where(s => s.IsEnabled).Select(s => s.Name));
        ScheduleCombo.ItemsSource = items;

        ScheduleCombo.SelectionChanged += (_, _) =>
            Settings.ScheduleName = ScheduleCombo.SelectedItem is string s && s != AnyScheduleLabel ? s : "";
        OffsetSpin.ValueChanged += (_, e) =>
            Settings.OffsetSeconds = (int)Math.Round(e.NewValue ?? 0);
        RequireDutySwitch.IsCheckedChanged += (_, _) =>
            Settings.RequireDutyToday = RequireDutySwitch.IsChecked == true;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_isLoaded) return;
        _isLoaded = true;

        var current = string.IsNullOrWhiteSpace(Settings.ScheduleName)
            ? AnyScheduleLabel
            : Settings.ScheduleName;
        ScheduleCombo.SelectedItem = ScheduleCombo.Items?.Contains(current) == true ? current : AnyScheduleLabel;

        OffsetSpin.Value = Settings.OffsetSeconds;
        RequireDutySwitch.IsChecked = Settings.RequireDutyToday;
    }
}
