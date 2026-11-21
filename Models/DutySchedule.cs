using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DutyRoster.Models;

/// <summary>值日时段，决定何时提醒、组件何时显示。</summary>
public class DutySchedule : INotifyPropertyChanged
{
    private string _name = "上课前";
    private TimeSpan _start = new(7, 30, 0);
    private TimeSpan _end = new(8, 0, 0);
    private bool _isEnabled = true;
    private bool _allowReminder = true;

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value ?? "");
    }

    public TimeSpan Start
    {
        get => _start;
        set
        {
            SetField(ref _start, value);
            OnPropertyChanged(nameof(StartText));
            OnPropertyChanged(nameof(TimeRangeText));
        }
    }

    public TimeSpan End
    {
        get => _end;
        set
        {
            SetField(ref _end, value);
            OnPropertyChanged(nameof(EndText));
            OnPropertyChanged(nameof(TimeRangeText));
        }
    }

    public string StartText
    {
        get => _start.ToString(@"hh\:mm");
        set
        {
            if (TryParseTime(value, out var ts)) Start = ts;
        }
    }

    public string EndText
    {
        get => _end.ToString(@"hh\:mm");
        set
        {
            if (TryParseTime(value, out var ts)) End = ts;
        }
    }

    public string TimeRangeText => $"{_start:hh\\:mm} – {_end:hh\\:mm}";

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetField(ref _isEnabled, value);
    }

    public bool AllowReminder
    {
        get => _allowReminder;
        set => SetField(ref _allowReminder, value);
    }

    /// <summary>判断给定时刻是否在时段内（含开始、不含结束）。</summary>
    public bool Contains(TimeOfDay now) => IsEnabled && now.Value >= _start && now.Value < _end;

    /// <summary>容错解析 "7:30"、"07:30"、"7点30" 等写法。</summary>
    public static bool TryParseTime(string? text, out TimeSpan result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var t = text.Trim().Replace('：', ':').Replace('点', ':').Replace('分', ' ').Trim();
        if (TimeSpan.TryParse(t, out var ts) && ts >= TimeSpan.Zero && ts < TimeSpan.FromDays(1))
        {
            result = ts;
            return true;
        }

        var parts = t.Split(':', ' ');
        if (parts.Length >= 2 &&
            int.TryParse(parts[0], out var h) &&
            int.TryParse(parts[1], out var m) &&
            h is >= 0 and < 24 && m is >= 0 and < 60)
        {
            result = new TimeSpan(h, m, 0);
            return true;
        }

        return false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>一天中的时刻包装。</summary>
public readonly record struct TimeOfDay(TimeSpan Value);
