using System.Collections.ObjectModel;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using DutyRoster.Models;
using DutyRoster.Models.Rotation;
using DutyRoster.Services;

namespace DutyRoster.Settings;

/// <summary>插件设置页：值日表编辑、轮换、时段、班级名单与其他选项。</summary>
[SettingsPageInfo(
    "dutylist.roster.settings",
    "值日提醒与排班",
    "\uE7C4",
    "\uE7C4")]
public partial class DutySettingsPage : SettingsPageBase
{
    private static readonly (DayOfWeek Day, string Label)[] WeekdayMap =
    [
        (DayOfWeek.Monday, "周一"),
        (DayOfWeek.Tuesday, "周二"),
        (DayOfWeek.Wednesday, "周三"),
        (DayOfWeek.Thursday, "周四"),
        (DayOfWeek.Friday, "周五"),
        (DayOfWeek.Saturday, "周六"),
        (DayOfWeek.Sunday, "周日")
    ];

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly ObservableCollection<RosterColumn> _columns = new();
    private readonly ObservableCollection<RosterRow> _rows = new();
    private readonly ObservableCollection<DutySchedule> _schedules = new();
    private readonly ObservableCollection<PreviewRow> _preview = new();
    private readonly Dictionary<DayOfWeek, CheckBox> _skipChecks = new();

    /// <summary>回填控件时抑制事件处理，避免把初始化误判为用户编辑。</summary>
    private bool _isLoading;

    /// <summary>
    /// 抑制「表格视图 → 配置」的回写。结构性增删（增删行/列）之后表格视图与配置
    /// 的下标已不一致，此刻回写会把刚删除的数据写回去，因此需要跳过。
    /// </summary>
    private bool _suppressApply;

    private DutyConfig Config => Plugin.Config;

    public DutySettingsPage()
    {
        InitializeComponent();

        _isLoading = true;

        HeaderItems.ItemsSource = _columns;
        RowsItems.ItemsSource = _rows;
        ScheduleItems.ItemsSource = _schedules;
        PreviewItems.ItemsSource = _preview;

        BuildSkipDayChecks();
        BuildPresetCombo();
        WireEvents();

        ReloadTable();
        LoadRotation();
        LoadSchedules();
        LoadStudents();

        _isLoading = false;
        RefreshPreview();
    }

    private void WireEvents()
    {
        TableNameBox.TextChanged += (_, _) =>
        {
            if (_isLoading) return;
            Config.Name = TableNameBox.Text ?? "";
            RefreshPreview();
        };

        EnabledSwitch.IsCheckedChanged += (_, _) =>
        {
            if (_isLoading) return;
            Config.IsEnabled = EnabledSwitch.IsChecked == true;
            Save();
        };

        NotifySwitch.IsCheckedChanged += (_, _) =>
        {
            if (_isLoading) return;
            Config.EnableNotification = NotifySwitch.IsChecked == true;
            Save();
        };

        ShowOnlyCurrentCheck.IsCheckedChanged += (_, _) =>
        {
            if (_isLoading) return;
            Config.ShowOnlyCurrentSchedule = ShowOnlyCurrentCheck.IsChecked == true;
            Save();
        };

        AddTypeBtn.Click += OnAddType;
        AddGroupBtn.Click += OnAddGroup;
        RemoveTypeBtn.Click += OnRemoveType;
        RemoveGroupBtn.Click += OnRemoveGroup;
        SaveBtn.Click += (_, _) => Save();
        ResetBtn.Click += OnReset;

        AddScheduleBtn.Click += OnAddSchedule;
        AddSkipDateBtn.Click += OnAddSkipDate;
        ClearSkipDatesBtn.Click += OnClearSkipDates;

        SetCurrentGroupBtn.Click += OnSetCurrentGroup;

        ImportStudentsBtn.Click += OnImportStudents;
        ExportStudentsBtn.Click += OnExportStudents;
        ClearStudentsBtn.Click += OnClearStudents;
        PickRandomBtn.Click += OnPickRandom;

        ExportBtn.Click += OnExportConfig;
        ImportBtn.Click += OnImportConfig;
    }

    // ===================== 值日表格 =====================

    /// <summary>从配置重建表格编辑区，并回填顶部选项。</summary>
    private void ReloadTable()
    {
        _isLoading = true;

        _columns.Clear();
        _rows.Clear();

        foreach (var group in Config.Groups)
            _columns.Add(new RosterColumn { GroupName = group.Name, IsEnabled = group.IsEnabled });

        foreach (var type in Config.Types)
        {
            var row = new RosterRow
            {
                TypeName = type.Name,
                Color = type.Color,
                RequiredPeople = type.RequiredPeople,
                IsEnabled = type.IsEnabled
            };

            foreach (var group in Config.Groups)
            {
                var assignment = group.Assignments.FirstOrDefault(
                    a => string.Equals(a.TypeName, type.Name, StringComparison.Ordinal));

                var cell = new RosterCell
                {
                    GroupName = group.Name,
                    TypeName = type.Name,
                    PeopleText = assignment is null ? "" : string.Join("、", assignment.People)
                };
                cell.Edited += (_, _) =>
                {
                    if (_isLoading) return;
                    RefreshPreview();
                };
                row.Cells.Add(cell);
            }

            _rows.Add(row);
        }

        HeaderGrid.IsVisible = _columns.Count > 0;
        TableNameBox.Text = Config.Name;
        RandomRosterHint.Text = Plugin.Roster
            .GenerateRandomRoster(new RandomRosterRequest(Config.Groups.Count, Config.ActiveTypes.ToList()))
            .Message;

        // 回填「其他」页的开关：必须在 _isLoading 期间，避免触发写回
        EnabledSwitch.IsChecked = Config.IsEnabled;
        NotifySwitch.IsChecked = Config.EnableNotification;
        ShowOnlyCurrentCheck.IsChecked = Config.ShowOnlyCurrentSchedule;

        _isLoading = false;
    }

    /// <summary>
    /// 把表格编辑结果写回配置。保存与预览都会调用，使预览反映未保存的编辑。
    /// </summary>
    private void ApplyTableToConfig()
    {
        if (_suppressApply) return;

        for (var i = 0; i < Config.Groups.Count && i < _columns.Count; i++)
        {
            Config.Groups[i].Name = _columns[i].GroupName;
            Config.Groups[i].IsEnabled = _columns[i].IsEnabled;
        }

        for (var i = 0; i < Config.Types.Count && i < _rows.Count; i++)
        {
            var type = Config.Types[i];
            var row = _rows[i];
            var oldName = type.Name;

            type.Name = row.TypeName;
            type.Color = row.Color;
            type.RequiredPeople = row.RequiredPeople;
            type.IsEnabled = row.IsEnabled;

            // 类型改名时同步各组的引用，否则人员数据会与新名字脱钩
            if (!string.Equals(oldName, type.Name, StringComparison.Ordinal))
            {
                foreach (var group in Config.Groups)
                {
                    foreach (var a in group.Assignments.Where(a => a.TypeName == oldName).ToList())
                        a.TypeName = type.Name;
                }
            }
        }

        for (var t = 0; t < _rows.Count && t < Config.Types.Count; t++)
        {
            var typeName = Config.Types[t].Name;
            var row = _rows[t];

            for (var g = 0; g < row.Cells.Count && g < Config.Groups.Count; g++)
            {
                var group = Config.Groups[g];
                var assignment = group.Assignments.FirstOrDefault(
                    a => string.Equals(a.TypeName, typeName, StringComparison.Ordinal));

                if (assignment is null)
                {
                    assignment = new GroupAssignment { TypeName = typeName };
                    group.Assignments.Add(assignment);
                }

                assignment.PeopleText = row.Cells[g].PeopleText;
            }
        }
    }

    private void OnAddType(object? sender, RoutedEventArgs e)
    {
        ApplyTableToConfig();

        var name = MakeUniqueName("新值日", Config.Types.Select(t => t.Name));
        Config.Types.Add(new DutyType { Name = name, Color = "#29B6F6", RequiredPeople = 1 });

        foreach (var group in Config.Groups)
            group.Assignments.Add(new GroupAssignment { TypeName = name });

        _suppressApply = true;
        Save();
        ReloadTable();
        _suppressApply = false;
    }

    private void OnAddGroup(object? sender, RoutedEventArgs e)
    {
        ApplyTableToConfig();

        var name = MakeUniqueName("新组", Config.Groups.Select(g => g.Name));
        var group = new DutyGroup { Name = name, IsEnabled = true };
        foreach (var type in Config.Types)
            group.Assignments.Add(new GroupAssignment { TypeName = type.Name });
        Config.Groups.Add(group);

        _suppressApply = true;
        Save();
        ReloadTable();
        _suppressApply = false;
    }

    /// <summary>删除当前在表格中高亮的那一行；没有选中时删除最后一行。</summary>
    private void OnRemoveType(object? sender, RoutedEventArgs e)
    {
        if (Config.Types.Count <= 1)
        {
            ShowStatus("至少保留一个值日类型。");
            return;
        }

        ApplyTableToConfig();

        var index = RowsItems.SelectedIndex;
        if (index < 0 || index >= Config.Types.Count) index = Config.Types.Count - 1;

        var removed = Config.Types[index];
        Config.Types.RemoveAt(index);

        foreach (var group in Config.Groups)
        {
            var assignment = group.Assignments.FirstOrDefault(a => a.TypeName == removed.Name);
            if (assignment is not null) group.Assignments.Remove(assignment);
        }

        // 删行后视图与配置下标错位，禁止回写后再重新按配置重建表格
        _suppressApply = true;
        Save();
        ReloadTable();
        _suppressApply = false;

        if (RowsItems.ItemCount > 0)
            RowsItems.SelectedIndex = Math.Min(index, RowsItems.ItemCount - 1);
    }

    /// <summary>删除当前在表头中高亮的那一列；没有选中时删除最后一列。</summary>
    private void OnRemoveGroup(object? sender, RoutedEventArgs e)
    {
        if (Config.Groups.Count <= 1)
        {
            ShowStatus("至少保留一个值日组。");
            return;
        }

        ApplyTableToConfig();

        var index = HeaderItems.SelectedIndex;
        if (index < 0 || index >= Config.Groups.Count) index = Config.Groups.Count - 1;

        Config.Groups.RemoveAt(index);

        _suppressApply = true;
        Save();
        ReloadTable();
        _suppressApply = false;

        if (HeaderItems.ItemCount > 0)
            HeaderItems.SelectedIndex = Math.Min(index, HeaderItems.ItemCount - 1);
    }

    private static string MakeUniqueName(string baseName, IEnumerable<string> existing)
    {
        var set = new HashSet<string>(existing, StringComparer.Ordinal);
        for (var i = 1; i < 1000; i++)
        {
            var candidate = $"{baseName}{i}";
            if (set.Add(candidate)) return candidate;
        }
        return baseName + Guid.NewGuid().ToString("N")[..4];
    }

    // ===================== 轮换 =====================

    private void BuildPresetCombo()
    {
        PresetCombo.ItemsSource = RotationPresetRegistry.All;

        PresetCombo.SelectionChanged += (_, _) =>
        {
            if (_isLoading) return;
            if (PresetCombo.SelectedItem is not IRotationPreset preset) return;
            Config.Rotation.PresetKind = preset.Kind;
            Save();
            LoadRotation();
        };

        BiweeklySpin.ValueChanged += (_, e) =>
        {
            if (_isLoading) return;
            Config.Rotation.BiweeklyHalfLength = (int)Math.Round(e.NewValue ?? 0);
            Save();
            RefreshPreview();
        };

        StartDatePicker.SelectedDateChanged += (_, e) =>
        {
            if (_isLoading) return;
            if (e.NewDate is not { } dto) return;
            Config.Rotation.StartDate = dto.Date;
            Save();
            UpdateRotationStatus();
            RefreshPreview();
        };
    }

    private void LoadRotation()
    {
        _isLoading = true;

        var preset = Config.Rotation.Preset;
        PresetDescText.Text = preset.Description;
        BiweeklyPanel.IsVisible = preset.Kind == RotationKinds.Biweekly;

        PresetCombo.SelectedItem = preset;
        BiweeklySpin.Value = Config.Rotation.BiweeklyHalfLength;
        StartDatePicker.SelectedDate = new DateTimeOffset(Config.Rotation.StartDate);

        foreach (var (day, box) in _skipChecks)
            box.IsChecked = Config.Rotation.SkipDays.Contains(day);

        _isLoading = false;
        RefreshSkipList();
    }

    private void BuildSkipDayChecks()
    {
        foreach (var (day, label) in WeekdayMap)
        {
            var box = new CheckBox { Content = label, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
            box.IsCheckedChanged += (_, _) =>
            {
                if (_isLoading) return;

                if (box.IsChecked == true)
                {
                    if (!Config.Rotation.SkipDays.Contains(day)) Config.Rotation.SkipDays.Add(day);
                }
                else
                {
                    Config.Rotation.SkipDays.Remove(day);
                }
                Save();
            };
            _skipChecks[day] = box;
            SkipDayPanel.Children.Add(box);
        }
    }

    private void OnAddSkipDate(object? sender, RoutedEventArgs e)
    {
        if (ExtraSkipPicker.SelectedDate is not { } dto) return;
        var date = dto.Date;
        if (Config.Rotation.ExtraSkipDates.Contains(date)) return;

        Config.Rotation.ExtraSkipDates.Add(date);
        Save();
        RefreshSkipList();
    }

    private void OnClearSkipDates(object? sender, RoutedEventArgs e)
    {
        Config.Rotation.ExtraSkipDates.Clear();
        Save();
        RefreshSkipList();
    }

    private void RefreshSkipList()
    {
        ExtraSkipList.ItemsSource = Config.Rotation.ExtraSkipDates
            .OrderBy(d => d)
            .Select(d => d.ToString("yyyy/M/d"))
            .ToList();
        UpdateRotationStatus();
        RefreshPreview();
    }

    private void UpdateRotationStatus()
    {
        var groups = Config.ActiveGroups;
        if (groups.Count == 0)
        {
            RotationStatusText.Text = "尚未配置值日组。";
            return;
        }

        var context = Config.Rotation.BuildContext(groups.Count);
        var today = DateTime.Today;
        var preset = Config.Rotation.Preset;
        var index = preset.ResolveGroupIndex(context, today);

        if (index < 0 || index >= groups.Count)
        {
            RotationStatusText.Text = "今日不排值日";
        }
        else
        {
            var groupName = groups[index].Name;
            RotationStatusText.Text = $"当前第 {index + 1} 组 · {groupName}";
        }

        // 回填手动设置框为当前组号
        _isLoading = true;
        if (index >= 0 && index < groups.Count)
            CurrentGroupSpin.Value = index + 1;
        _isLoading = false;
    }

    /// <summary>手动把「当前」设为第 X 组，换算为组偏移量。</summary>
    private void OnSetCurrentGroup(object? sender, RoutedEventArgs e)
    {
        var groups = Config.ActiveGroups;
        if (groups.Count == 0) return;

        var target = (int)Math.Round(CurrentGroupSpin.Value ?? 1);
        Config.Rotation.SetCurrentGroupNumber(target, groups.Count);

        Save();
        UpdateRotationStatus();
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        _preview.Clear();

        ApplyTableToConfig();
        var groups = Config.ActiveGroups;

        if (groups.Count == 0)
        {
            UpdateRotationStatus();
            return;
        }

        var context = Config.Rotation.BuildContext(groups.Count);
        var preset = Config.Rotation.Preset;
        var today = DateTime.Today;

        for (var i = 0; i < 14; i++)
        {
            var date = today.AddDays(i);
            var index = preset.ResolveGroupIndex(context, date);
            var groupName = index >= 0 && index < groups.Count ? groups[index].Name : "休息";

            _preview.Add(new PreviewRow
            {
                Date = date.ToString("M/d"),
                Weekday = WeekdayMap[((int)date.DayOfWeek + 6) % 7].Label,
                Group = groupName
            });
        }

        UpdateRotationStatus();
    }

    // ===================== 时段 =====================

    private void LoadSchedules()
    {
        _schedules.Clear();
        foreach (var s in Config.Schedules) _schedules.Add(s);
    }

    private void OnAddSchedule(object? sender, RoutedEventArgs e)
    {
        var schedule = new DutySchedule { Name = $"时段{Config.Schedules.Count + 1}" };
        Config.Schedules.Add(schedule);
        _schedules.Add(schedule);
        Save();
    }

    private void OnDeleteSchedule(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: DutySchedule schedule }) return;

        Config.Schedules.Remove(schedule);
        _schedules.Remove(schedule);
        Save();
    }

    // ===================== 班级名单 =====================

    private void LoadStudents()
    {
        _isLoading = true;
        StudentsBox.Text = string.Join(Environment.NewLine,
            Config.Students.Select(s =>
                string.IsNullOrWhiteSpace(s.Number) ? s.Name : $"{s.Number} {s.Name}"));
        _isLoading = false;
        UpdateStudentCount();
    }

    private void UpdateStudentCount()
        => StudentsCountText.Text = $"名单共 {Config.Students.Count} 人";

    private void ApplyStudentsFromBox()
    {
        Config.Students.Clear();
        var index = 1;
        foreach (var line in (StudentsBox.Text ?? "").Split('\n'))
        {
            var text = line.Trim().TrimEnd('\r');
            if (text.Length == 0) continue;

            var parts = text.Split([' ', '\t', '、', ','], 2, StringSplitOptions.RemoveEmptyEntries);
            var hasNumber = parts.Length == 2 && parts[0].All(char.IsDigit);
            Config.Students.Add(new Student
            {
                Number = hasNumber ? parts[0] : index.ToString("00"),
                Name = hasNumber ? parts[1].Trim() : text
            });
            index++;
        }

        Save();
        UpdateStudentCount();
    }

    private async void OnImportStudents(object? sender, RoutedEventArgs e)
    {
        var files = await GetStorageProvider().OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "导入班级名单",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("文本文件") { Patterns = ["*.txt", "*.csv"] }]
        });

        if (files.Count == 0) return;

        try
        {
            StudentsBox.Text = await ReadAllTextAsync(files[0]);
            ApplyStudentsFromBox();
            ShowStatus("名单已导入。");
        }
        catch (Exception ex)
        {
            ShowStatus($"导入失败：{ex.Message}");
        }
    }

    private async void OnExportStudents(object? sender, RoutedEventArgs e)
    {
        var file = await GetStorageProvider().SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出班级名单",
            SuggestedFileName = "班级名单.txt",
            DefaultExtension = "txt",
            FileTypeChoices = [new FilePickerFileType("文本文件") { Patterns = ["*.txt"] }]
        });

        if (file is null) return;

        try
        {
            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream, Utf8NoBom);
            await writer.WriteAsync(StudentsBox.Text ?? "");
            ShowStatus("名单已导出。");
        }
        catch (Exception ex)
        {
            ShowStatus($"导出失败：{ex.Message}");
        }
    }

    private void OnClearStudents(object? sender, RoutedEventArgs e)
    {
        StudentsBox.Text = "";
        ApplyStudentsFromBox();
        PickResultText.Text = "";
        ShowStatus("名单已清空。");
    }

    private void OnPickRandom(object? sender, RoutedEventArgs e)
    {
        ApplyStudentsFromBox();

        // 每次点名前把配置里的名单灌入服务，保证用的是最新编辑
        if (Plugin.Roster is ClassRosterService roster)
        {
            roster.Clear();
            foreach (var s in Config.Students.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
                roster.ImportFromText(s.Name, true);
        }

        var picked = Plugin.Roster.PickRandomStudents(1);
        PickResultText.Text = picked.Count == 0
            ? "名单为空，无法点名。"
            : $"点到：{string.Join("、", picked.Select(p => p.Name))}";
    }

    // ===================== 导入 / 导出 =====================

    private async void OnExportConfig(object? sender, RoutedEventArgs e)
    {
        ApplyTableToConfig();

        var file = await GetStorageProvider().SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出值日表",
            SuggestedFileName = "值日表.json",
            DefaultExtension = "json",
            FileTypeChoices = [new FilePickerFileType("JSON") { Patterns = ["*.json"] }]
        });

        if (file is null) return;

        try
        {
            var json = JsonSerializer.Serialize(Config, SerializerOptions);
            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream, Utf8NoBom);
            await writer.WriteAsync(json);
            ShowStatus("值日表已导出。");
        }
        catch (Exception ex)
        {
            ShowStatus($"导出失败：{ex.Message}");
        }
    }

    private async void OnImportConfig(object? sender, RoutedEventArgs e)
    {
        var files = await GetStorageProvider().OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "导入值日表",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("JSON") { Patterns = ["*.json"] }]
        });

        if (files.Count == 0) return;

        try
        {
            var text = await ReadAllTextAsync(files[0]);
            var imported = JsonSerializer.Deserialize<DutyConfig>(text, SerializerOptions);
            if (imported is null)
            {
                ShowStatus("文件内容为空或格式不正确。");
                return;
            }

            var target = Plugin.ConfigService.Config;
            if (imported.Types is { Count: > 0 }) target.Types = imported.Types;
            if (imported.Groups is { Count: > 0 }) target.Groups = imported.Groups;
            if (imported.Schedules is { Count: > 0 }) target.Schedules = imported.Schedules;
            if (imported.Students is { Count: > 0 }) target.Students = imported.Students;
            if (imported.Rotation is not null) target.Rotation = imported.Rotation;
            if (!string.IsNullOrWhiteSpace(imported.Name)) target.Name = imported.Name;

            Save();

            _isLoading = true;
            ReloadTable();
            LoadRotation();
            LoadSchedules();
            LoadStudents();
            TableNameBox.Text = target.Name;
            _isLoading = false;
            RefreshPreview();

            ShowStatus("值日表已导入。");
        }
        catch (Exception ex)
        {
            ShowStatus($"导入失败：{ex.Message}");
        }
    }

    // ===================== 保存 =====================

    private void Save()
    {
        ApplyTableToConfig();
        Plugin.SaveAndNotify();
        UpdateRotationStatus();
    }

    private void OnReset(object? sender, RoutedEventArgs e)
    {
        Plugin.ResetToDefault();

        _isLoading = true;
        ReloadTable();
        LoadRotation();
        LoadSchedules();
        LoadStudents();
        TableNameBox.Text = Config.Name;
        _isLoading = false;
        RefreshPreview();

        ShowStatus("已重置为空白值日表。");
    }

    private void ShowStatus(string message) => RandomRosterHint.Text = message;

    private IStorageProvider GetStorageProvider()
        => TopLevel.GetTopLevel(this)!.StorageProvider;

    /// <summary>UTF-8（无 BOM）。中文必须显式指定，否则在 GBK 系统上会乱码。</summary>
    private static readonly System.Text.Encoding Utf8NoBom = new System.Text.UTF8Encoding(false);

    private static async Task<string> ReadAllTextAsync(IStorageFile file)
    {
        await using var stream = await file.OpenReadAsync();
        using var reader = new StreamReader(stream, Utf8NoBom);
        return await reader.ReadToEndAsync();
    }
}