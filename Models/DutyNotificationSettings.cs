using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DutyRoster.Models;

/// <summary>值日提醒提供方的设置，由 ClassIsland 原生提醒设置界面持久化。</summary>
public class DutyNotificationSettings : INotifyPropertyChanged
{
    private bool _isEnabled = true;
    private int _leadSeconds;
    private string _contentTemplate = "{types}";
    private string _maskTemplate = "{group} 值日";
    private bool _isSpeechEnabled = true;
    private bool _onlyCurrentSchedule = true;

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetField(ref _isEnabled, value);
    }

    /// <summary>提前多少秒提醒，0 表示到点提醒。</summary>
    public int LeadSeconds
    {
        get => _leadSeconds;
        set => SetField(ref _leadSeconds, Math.Max(0, value));
    }

    public string MaskTemplate
    {
        get => _maskTemplate;
        set => SetField(ref _maskTemplate, value ?? "");
    }

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

    public bool OnlyCurrentSchedule
    {
        get => _onlyCurrentSchedule;
        set => SetField(ref _onlyCurrentSchedule, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
