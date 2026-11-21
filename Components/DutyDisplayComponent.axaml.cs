using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using DutyRoster.Models;
using DutyRoster.Services;

namespace DutyRoster.Components;

/// <summary>主界面值日生组件，显示当前值日组及各项任务。</summary>
[ComponentInfo(
    "8F6C2B41-7D3A-4E92-9C18-3B5A6D0E7F21",
    "值日提醒与排班",
    "\uE7C4",
    "显示当前值日组的各项值日任务")]
public partial class DutyDisplayComponent : ComponentBase<DutyDisplayComponentSettings>
{
    private readonly DispatcherTimer _refreshTimer = new() { Interval = TimeSpan.FromSeconds(20) };

    public DutyDisplayComponent()
    {
        InitializeComponent();

        AttachedToVisualTree += (_, _) =>
        {
            Plugin.ConfigChanged += OnConfigChanged;
            Render();
            _refreshTimer.Start();
        };

        DetachedFromVisualTree += (_, _) =>
        {
            Plugin.ConfigChanged -= OnConfigChanged;
            _refreshTimer.Stop();
        };

        _refreshTimer.Tick += (_, _) => Render();
        Render();
    }

    private void OnConfigChanged(object? sender, EventArgs e)
        => Dispatcher.UIThread.Post(Render);

    private void Render()
    {
        if (StackRoot is null) return;

        StackRoot.Children.Clear();

        var config = Plugin.Config;
        var today = DateTime.Today;

        if (!config.IsEnabled)
        {
            ShowEmpty("值日提醒与排班未启用");
            return;
        }

        var group = config.ResolveGroup(today);
        if (group is null)
        {
            var reason = config.ActiveGroups.Count == 0
                ? "尚未配置值日组"
                : config.Rotation.SkipDays.Contains(today.DayOfWeek)
                    ? "今日休息"
                    : "今日不排值日";
            ShowEmpty(reason);
            return;
        }

        var types = config.ActiveTypes.ToList();
        if (types.Count == 0)
        {
            ShowEmpty("尚未配置值日类型");
            return;
        }

        // 组件设置里勾选了「只展示哪些类型」时，过滤出可见类型
        var visibleSet = Settings?.GetVisibleTypeSet();
        if (visibleSet is { Count: > 0 })
        {
            types = types.Where(t => visibleSet.Contains(t.Name)).ToList();
            if (types.Count == 0)
            {
                ShowEmpty("未勾选要展示的值日类型");
                return;
            }
        }

        // 仅在值日时段内显示时，需要判断当前是否处于某个时段
        DutySchedule? currentSchedule = null;
        if (config.ShowOnlyCurrentSchedule)
        {
            currentSchedule = config.ResolveCurrentSchedule(DateTime.Now);
            if (currentSchedule is null)
            {
                ShowEmpty("当前没有值日时段");
                return;
            }
        }

        HideEmpty();
        AddGroupBadge(group, currentSchedule);

        foreach (var type in types)
            AddTaskBlock(group, type, types.Count);
    }

    private void AddGroupBadge(DutyGroup group, DutySchedule? schedule)
    {
        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 2
        };

        // 组名颜色：优先用组件设置，空则回退白色
        var groupBrush = Settings != null && !string.IsNullOrWhiteSpace(Settings.GroupNameColor)
            ? ResolveBrush(Settings.GroupNameColor, 1.0)
            : ResolveBrush(null, 0.95);

        stack.Children.Add(new TextBlock
        {
            Text = group.Name,
            FontSize = 16,
            FontWeight = FontWeight.SemiBold,
            Foreground = groupBrush
        });

        if (schedule is not null)
        {
            stack.Children.Add(new TextBlock
            {
                Text = schedule.Name,
                FontSize = 11,
                Opacity = 0.7,
                Foreground = ResolveBrush(null, 0.8)
            });
        }

        StackRoot.Children.Add(stack);
        StackRoot.Children.Add(CreateSeparator());
    }

    private void AddTaskBlock(DutyGroup group, DutyType type, int totalTypes)
    {
        var people = DutyFormatter.GetPeople(group, type.Name);
        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 2
        };

        // 类型标签颜色：优先用组件设置，空则回退浅灰
        var labelBrush = Settings != null && !string.IsNullOrWhiteSpace(Settings.TypeLabelColor)
            ? ResolveBrush(Settings.TypeLabelColor, 1.0)
            : ResolveBrush(null, 0.8);

        var header = string.IsNullOrWhiteSpace(type.Icon) ? type.Name : $"{type.Icon} {type.Name}";
        stack.Children.Add(new TextBlock
        {
            Text = header,
            FontSize = 11,
            Opacity = 0.75,
            Foreground = labelBrush
        });

        stack.Children.Add(new TextBlock
        {
            Text = people.Count == 0 ? "（未安排）" : string.Join("、", people),
            FontSize = 15,
            FontWeight = FontWeight.Medium,
            TextWrapping = TextWrapping.NoWrap,
            Foreground = ResolveBrush(type.Color, 1.0)
        });

        StackRoot.Children.Add(stack);

        if (StackRoot.Children.Count < totalTypes * 2)
            StackRoot.Children.Add(CreateSeparator());
    }

    private Control CreateSeparator() => new Border
    {
        Width = 1,
        Margin = new Thickness(2, 6),
        Background = ResolveBrush(null, 0.25),
        VerticalAlignment = VerticalAlignment.Stretch
    };

    private static IBrush ResolveBrush(string? hexColor, double opacity)
    {
        if (string.IsNullOrWhiteSpace(hexColor) || !Color.TryParse(hexColor, out var color))
            color = Color.Parse("#FFFFFF");

        return new SolidColorBrush(color, opacity);
    }

    private void ShowEmpty(string message)
    {
        if (EmptyText is null) return;
        EmptyText.Text = message;
        EmptyText.IsVisible = true;
        if (StackRoot is not null) StackRoot.IsVisible = false;
    }

    private void HideEmpty()
    {
        if (EmptyText is null) return;
        EmptyText.IsVisible = false;
        if (StackRoot is not null) StackRoot.IsVisible = true;
    }
}
