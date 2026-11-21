using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DutyRoster.Models;

/// <summary>班级名单中的一位同学。</summary>
public class Student : INotifyPropertyChanged
{
    private string _name = "";
    private string _number = "";
    private string _note = "";
    private bool _isExcluded;

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value ?? "");
    }

    public string Number
    {
        get => _number;
        set => SetField(ref _number, value ?? "");
    }

    public string Note
    {
        get => _note;
        set => SetField(ref _note, value ?? "");
    }

    /// <summary>排除在随机排班 / 点名之外（如请假）。</summary>
    public bool IsExcluded
    {
        get => _isExcluded;
        set => SetField(ref _isExcluded, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
