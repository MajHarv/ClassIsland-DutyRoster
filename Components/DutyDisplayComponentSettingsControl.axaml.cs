using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;

namespace DutyRoster.Components;

/// <summary>值日生组件的设置界面：勾选展示类型 + 自定义文字颜色。</summary>
public partial class DutyDisplayComponentSettingsControl : ComponentBase<DutyDisplayComponentSettings>
{
    private readonly ObservableCollection<TypeCheckItem> _items = new();
    private bool _isLoading;

    /// <summary>复选框项。</summary>
    public sealed class TypeCheckItem : INotifyPropertyChanged
    {
        public string Name { get; init; } = "";

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked == value) return;
                _isChecked = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? Changed;
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public DutyDisplayComponentSettingsControl()
    {
        InitializeComponent();

        TypeCheckList.ItemsSource = _items;
        SelectAllBtn.Click += (_, _) => SetAll(true);
        SelectNoneBtn.Click += (_, _) => SetAll(false);

        ClearGroupColorBtn.Click += (_, _) => SetGroupColor(null);
        ClearTypeColorBtn.Click += (_, _) => SetTypeColor(null);

        GroupNameColorPicker.ColorChanged += (_, _) => ApplyGroupColorFromPicker();
        TypeLabelColorPicker.ColorChanged += (_, _) => ApplyTypeColorFromPicker();
    }

    /// <summary>
    /// 必须在挂载到视觉树后初始化：此时 SettingsInternal 才被宿主赋值，
    /// 构造函数里访问 Settings 会得到 null。
    /// </summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_items.Count == 0) Rebuild();
    }

    private void Rebuild()
    {
        _isLoading = true;
        _items.Clear();

        var visibleSet = Settings.GetVisibleTypeSet();
        var showAll = visibleSet.Count == 0;

        foreach (var type in Plugin.Config.ActiveTypes)
        {
            var item = new TypeCheckItem
            {
                Name = type.Name,
                IsChecked = showAll || visibleSet.Contains(type.Name)
            };
            item.Changed += (_, _) => Apply();
            _items.Add(item);
        }

        // 回填颜色选择器（ColorPicker.Color 非空，用白色作占位）
        GroupNameColorPicker.Color = ParseColor(Settings.GroupNameColor) ?? Color.Parse("#FFFFFF");
        TypeLabelColorPicker.Color = ParseColor(Settings.TypeLabelColor) ?? Color.Parse("#CCCCCC");

        _isLoading = false;
    }

    private void Apply()
    {
        if (_isLoading) return;

        var visible = _items.Where(i => i.IsChecked).Select(i => i.Name).ToList();

        // 全部勾选 = 显示全部（存空串）；否则存勾选的类型名
        Settings.VisibleTypeNames = visible.Count == _items.Count
            ? ""
            : string.Join("、", visible);
    }

    private void SetAll(bool check)
    {
        foreach (var item in _items) item.IsChecked = check;
    }

    private void SetGroupColor(Color? color)
    {
        if (color is null)
        {
            // 还原默认：清空设置，并把 picker 显示为默认白
            _isLoading = true;
            Settings.GroupNameColor = "";
            GroupNameColorPicker.Color = Color.Parse("#FFFFFF");
            _isLoading = false;
            return;
        }

        GroupNameColorPicker.Color = color.Value;
        Settings.GroupNameColor = ToHex(color);
    }

    private void SetTypeColor(Color? color)
    {
        if (color is null)
        {
            _isLoading = true;
            Settings.TypeLabelColor = "";
            TypeLabelColorPicker.Color = Color.Parse("#CCCCCC");
            _isLoading = false;
            return;
        }

        TypeLabelColorPicker.Color = color.Value;
        Settings.TypeLabelColor = ToHex(color);
    }

    private void ApplyGroupColorFromPicker()
    {
        if (_isLoading) return;
        Settings.GroupNameColor = ToHex(GroupNameColorPicker.Color);
    }

    private void ApplyTypeColorFromPicker()
    {
        if (_isLoading) return;
        Settings.TypeLabelColor = ToHex(TypeLabelColorPicker.Color);
    }

    /// <summary>Color → "#RRGGBB"；null 返回空串（表示默认）。</summary>
    private static string ToHex(Color? color)
    {
        if (color is null) return "";
        var c = color.Value;
        return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }

    /// <summary>"#RRGGBB" → Color；空/非法返回 null。</summary>
    private static Color? ParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        return Color.TryParse(hex, out var c) ? c : null;
    }
}
