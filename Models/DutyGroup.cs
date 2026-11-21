using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace DutyRoster.Models;

/// <summary>值日组，对应值日表的一列。</summary>
public class DutyGroup : INotifyPropertyChanged
{
    private string _name = "";
    private bool _isEnabled = true;

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value ?? "");
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetField(ref _isEnabled, value);
    }

    /// <summary>该组在各类值日下的分配，TypeName 与配置中的类型名对应。</summary>
    public ObservableCollection<GroupAssignment> Assignments { get; set; } = new();

    public GroupAssignment GetAssignment(string typeName)
    {
        foreach (var a in Assignments)
        {
            if (string.Equals(a.TypeName, typeName, StringComparison.Ordinal))
                return a;
        }
        var created = new GroupAssignment { TypeName = typeName };
        Assignments.Add(created);
        return created;
    }

    [JsonIgnore]
    public string PeopleSummary => string.Join("、",
        Assignments.SelectMany(a => a.People).Distinct());

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

/// <summary>某组在某值日类型下的人员。</summary>
public class GroupAssignment : INotifyPropertyChanged
{
    private string _typeName = "";
    private ObservableCollection<string> _people = new();

    public string TypeName
    {
        get => _typeName;
        set => SetField(ref _typeName, value ?? "");
    }

    public ObservableCollection<string> People
    {
        get => _people;
        set
        {
            if (_people == value) return;
            _people = value ?? new();
            OnPropertyChanged();
            OnPropertyChanged(nameof(PeopleText));
        }
    }

    /// <summary>顿号分隔的人员串，供文本框绑定。</summary>
    public string PeopleText
    {
        get => string.Join("、", _people);
        set
        {
            var parsed = PeopleParser.Parse(value);
            if (parsed.SequenceEqual(_people)) return;
            _people.Clear();
            foreach (var p in parsed) _people.Add(p);
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

/// <summary>姓名串解析，支持中英文顿号、逗号、斜杠、空格等分隔。</summary>
public static class PeopleParser
{
    private static readonly char[] Separators = ['、', '，', ',', '；', ';', '/', '｜', '|', '\n', '\t', ' '];

    public static List<string> Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();
        return text
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 0)
            .ToList();
    }
}
