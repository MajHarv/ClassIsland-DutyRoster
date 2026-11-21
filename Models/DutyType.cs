using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace DutyRoster.Models;

/// <summary>值日类型，对应值日表的一行。</summary>
public class DutyType : INotifyPropertyChanged
{
    private string _name = "";
    private int _requiredPeople = 2;
    private string _color = "#00BFFF";
    private bool _isEnabled = true;
    private string _icon = "";

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value ?? "");
    }

    /// <summary>每组应安排几人，0 表示不限。</summary>
    public int RequiredPeople
    {
        get => _requiredPeople;
        set => SetField(ref _requiredPeople, Math.Max(0, value));
    }

    public string Color
    {
        get => _color;
        set => SetField(ref _color, string.IsNullOrWhiteSpace(value) ? "#00BFFF" : value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetField(ref _isEnabled, value);
    }

    public string Icon
    {
        get => _icon;
        set => SetField(ref _icon, value ?? "");
    }

    [JsonIgnore]
    public string RequirementText => _requiredPeople > 0 ? $"{_requiredPeople} 人" : "不限";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
