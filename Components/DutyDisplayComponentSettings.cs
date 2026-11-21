using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DutyRoster.Components;

/// <summary>值日生组件的设置。</summary>
public class DutyDisplayComponentSettings : INotifyPropertyChanged
{
    private string _visibleTypeNames = "";
    private string _groupNameColor = "";
    private string _typeLabelColor = "";

    /// <summary>
    /// 要展示的值日类型名，用顿号分隔。留空表示展示全部类型。
    /// 例如「扫地、擦黑板」表示只显示这两类。
    /// </summary>
    public string VisibleTypeNames
    {
        get => _visibleTypeNames;
        set => SetField(ref _visibleTypeNames, value ?? "");
    }

    /// <summary>组名（如「第一组」）的颜色，留空表示自动（白色）。</summary>
    public string GroupNameColor
    {
        get => _groupNameColor;
        set => SetField(ref _groupNameColor, value ?? "");
    }

    /// <summary>值日类型标签（如「扫地」）的颜色，留空表示自动（浅灰）。</summary>
    public string TypeLabelColor
    {
        get => _typeLabelColor;
        set => SetField(ref _typeLabelColor, value ?? "");
    }

    /// <summary>解析出勾选要展示的类型名集合（去重、去空白）。</summary>
    public HashSet<string> GetVisibleTypeSet()
    {
        if (string.IsNullOrWhiteSpace(_visibleTypeNames)) return new HashSet<string>();
        return _visibleTypeNames
            .Split('、', ',', '，')
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
