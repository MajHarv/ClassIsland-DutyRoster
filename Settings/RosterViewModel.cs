using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace DutyRoster.Settings;

/// <summary>值日表格的一个人员单元格。</summary>
public class RosterCell : INotifyPropertyChanged
{
    private string _peopleText = "";

    public string GroupName { get; init; } = "";

    public string TypeName { get; init; } = "";

    public string PeopleText
    {
        get => _peopleText;
        set
        {
            if (_peopleText == value) return;
            _peopleText = value ?? "";
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PeopleText)));
            Edited?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? Edited;

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>值日表格的一行（一个值日类型）。</summary>
public class RosterRow : INotifyPropertyChanged
{
    private string _typeName = "";
    private string _color = "#4CAF50";
    private int _requiredPeople;
    private bool _isEnabled = true;

    public string TypeName
    {
        get => _typeName;
        set
        {
            if (_typeName == value) return;
            _typeName = value ?? "";
            OnPropertyChanged();
        }
    }

    /// <summary>颜色值（#RRGGBB）。</summary>
    public string Color
    {
        get => _color;
        set
        {
            if (_color == value) return;
            _color = string.IsNullOrWhiteSpace(value) ? "#4CAF50" : value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ColorBrush));
            OnPropertyChanged(nameof(LabelForeground));
            OnPropertyChanged(nameof(SelectedColorOption));
        }
    }

    /// <summary>颜色下拉框的可选颜色。</summary>
    public IReadOnlyList<ColorOption> ColorOptions { get; } =
        DutyPalette.Palette.Select(c => new ColorOption(c)).ToList();

    /// <summary>下拉框当前选中的颜色。</summary>
    public ColorOption? SelectedColorOption
    {
        get => ColorOptions.FirstOrDefault(o => string.Equals(o.Hex, _color, StringComparison.OrdinalIgnoreCase));
        set
        {
            if (value is null) return;
            Color = value.Hex;
        }
    }

    /// <summary>行头色块使用的画刷。</summary>
    public IBrush ColorBrush => new SolidColorBrush(DutyPalette.Parse(_color, Avalonia.Media.Color.Parse("#4CAF50")));

    /// <summary>色块上的文字颜色，自动适配明暗。</summary>
    public IBrush LabelForeground => DutyPalette.ForegroundFor(_color);

    public int RequiredPeople
    {
        get => _requiredPeople;
        set
        {
            if (_requiredPeople == value) return;
            _requiredPeople = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PeopleHint));
        }
    }

    /// <summary>人数提示，如「2 人」。</summary>
    public string PeopleHint => _requiredPeople > 0 ? $"{_requiredPeople} 人" : "不限";

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value) return;
            _isEnabled = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<RosterCell> Cells { get; } = new();

    public override string ToString() => _typeName;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>值日表格的列头（一个值日组）。</summary>
public class RosterColumn : INotifyPropertyChanged
{
    private string _groupName = "";
    private bool _isEnabled = true;

    public string GroupName
    {
        get => _groupName;
        set
        {
            if (_groupName == value) return;
            _groupName = value ?? "";
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GroupName)));
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value) return;
            _isEnabled = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
        }
    }

    public override string ToString() => _groupName;

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>轮换预览的一行。</summary>
public class PreviewRow
{
    public string Date { get; init; } = "";
    public string Weekday { get; init; } = "";
    public string Group { get; init; } = "";
}

/// <summary>颜色下拉框的一项。</summary>
public class ColorOption
{
    public ColorOption(string hex) => Hex = hex;

    public string Hex { get; }

    public IBrush Brush => new SolidColorBrush(DutyPalette.Parse(Hex, Colors.Gray));

    public override string ToString() => Hex;
}
