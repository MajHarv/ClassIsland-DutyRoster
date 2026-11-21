using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DutyRoster.Automation;

/// <summary>「值日提醒」自动化行动的设置。</summary>
public class DutyRemindActionSettings : INotifyPropertyChanged
{
    private string _dutyTypeName = "";
    private bool _useCurrentGroup = true;
    private int _groupIndex = 1;
    private string _maskTemplate = "{group} 值日";
    private string _contentTemplate = "{types}";
    private bool _isSpeechEnabled = true;
    private double _maskDurationSeconds = 4.0;
    private double _contentDurationSeconds = 8.0;

    /// <summary>要提醒的值日类型名称，留空表示提醒全部类型。</summary>
    public string DutyTypeName
    {
        get => _dutyTypeName;
        set => SetField(ref _dutyTypeName, value ?? "");
    }

    /// <summary>是否提醒当前轮到的组（false 则用下面手动指定的组）。</summary>
    public bool UseCurrentGroup
    {
        get => _useCurrentGroup;
        set => SetField(ref _useCurrentGroup, value);
    }

    /// <summary>手动指定的组序号（从 1 开始），仅在 UseCurrentGroup 为 false 时生效。</summary>
    public int GroupIndex
    {
        get => _groupIndex;
        set => SetField(ref _groupIndex, Math.Max(1, value));
    }

    public string MaskTemplate
    {
        get => _maskTemplate;
        set => SetField(ref _maskTemplate, value ?? "");
    }

    /// <summary>正文模板，占位符同 DutyFormatter。默认 {types} 为单行。</summary>
    public string ContentTemplate
    {
        get => _contentTemplate;
        set => SetField(ref _contentTemplate, value ?? "");
    }

    public bool IsSpeechEnabled
    {
        get => _isSpeechEnabled;
        set => SetField(ref _isSpeechEnabled, value);
    }

    /// <summary>遮罩（标题）显示时长，单位秒。</summary>
    public double MaskDurationSeconds
    {
        get => _maskDurationSeconds;
        set => SetField(ref _maskDurationSeconds, Math.Max(0.5, value));
    }

    /// <summary>正文显示时长，单位秒。</summary>
    public double ContentDurationSeconds
    {
        get => _contentDurationSeconds;
        set => SetField(ref _contentDurationSeconds, Math.Max(0.5, value));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
