using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using DutyRoster.Models.Rotation;

namespace DutyRoster.Models;

/// <summary>插件根配置，以 JSON 保存。</summary>
public class DutyConfig : INotifyPropertyChanged
{
    private string _name = "值日表";
    private RotationSettings _rotation = new();
    private bool _isEnabled = true;
    private bool _showAllGroupsToday;
    private bool _showOnlyCurrentSchedule = true;
    private bool _enableNotification = true;
    private string _notificationTitleTemplate = "值日提醒";
    private string _notificationTemplate = "{types}";
    private int _configVersion = 1;

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value ?? "");
    }

    public int ConfigVersion
    {
        get => _configVersion;
        set => SetField(ref _configVersion, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetField(ref _isEnabled, value);
    }

    /// <summary>值日类型，表格的行。</summary>
    public ObservableCollection<DutyType> Types { get; set; } = new();

    /// <summary>值日组，表格的列。</summary>
    public ObservableCollection<DutyGroup> Groups { get; set; } = new();

    public RotationSettings Rotation
    {
        get => _rotation;
        set => SetField(ref _rotation, value ?? new());
    }

    public ObservableCollection<DutySchedule> Schedules { get; set; } = new();

    public ObservableCollection<Student> Students { get; set; } = new();

    public bool ShowAllGroupsToday
    {
        get => _showAllGroupsToday;
        set => SetField(ref _showAllGroupsToday, value);
    }

    public bool ShowOnlyCurrentSchedule
    {
        get => _showOnlyCurrentSchedule;
        set => SetField(ref _showOnlyCurrentSchedule, value);
    }

    /// <summary>本插件是否发送提醒，还需在 ClassIsland 提醒设置中启用。</summary>
    public bool EnableNotification
    {
        get => _enableNotification;
        set => SetField(ref _enableNotification, value);
    }

    /// <summary>提醒标题模板，占位符见 DutyFormatter。</summary>
    public string NotificationTitleTemplate
    {
        get => _notificationTitleTemplate;
        set => SetField(ref _notificationTitleTemplate, value ?? "");
    }

    public string NotificationTemplate
    {
        get => _notificationTemplate;
        set => SetField(ref _notificationTemplate, value ?? "");
    }

    [JsonIgnore]
    public IReadOnlyList<DutyGroup> ActiveGroups => Groups.Where(g => g.IsEnabled).ToList();

    [JsonIgnore]
    public IReadOnlyList<DutyType> ActiveTypes => Types.Where(t => t.IsEnabled).ToList();

    [JsonIgnore]
    public IRotationPreset Preset => Rotation.Preset;

    /// <summary>计算指定日期应显示哪一组，当天不排值日返回 null。</summary>
    public DutyGroup? ResolveGroup(DateTime date)
    {
        if (!IsEnabled) return null;

        var groups = ActiveGroups;
        if (groups.Count == 0) return null;

        var index = Preset.ResolveGroupIndex(Rotation.BuildContext(groups.Count), date);
        return index < 0 || index >= groups.Count ? null : groups[index];
    }

    /// <summary>返回今天轮到第几组（从 1 开始），0 表示不排值日。</summary>
    public int ResolveGroupNumber(DateTime date)
    {
        var groups = ActiveGroups;
        if (groups.Count == 0 || !IsEnabled) return 0;

        var index = Preset.ResolveGroupIndex(Rotation.BuildContext(groups.Count), date);
        return index < 0 ? 0 : index + 1;
    }

    /// <summary>当前所处时段，不在任何时段内返回 null。</summary>
    public DutySchedule? ResolveCurrentSchedule(DateTime now)
    {
        var t = now.TimeOfDay;
        foreach (var s in Schedules)
        {
            if (s.IsEnabled && t >= s.Start && t < s.End) return s;
        }
        return null;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
