using Avalonia;
using ClassIsland.Core.Abstractions.Controls;

namespace DutyRoster.Automation;

/// <summary>「值日提醒」行动的设置界面。</summary>
public partial class DutyRemindActionSettingsControl : ActionSettingsControlBase<DutyRemindActionSettings>
{
    private bool _isLoaded;

    public DutyRemindActionSettingsControl()
    {
        InitializeComponent();

        // 值日类型下拉框：留空 = 全部，加上每个启用类型
        var items = new List<string> { "（全部）" };
        items.AddRange(Plugin.Config.ActiveTypes.Select(t => t.Name));
        DutyTypeCombo.ItemsSource = items;

        // 事件里访问 Settings 是延迟的，安全
        UseCurrentGroupSwitch.IsCheckedChanged += (_, _) =>
        {
            var useCurrent = UseCurrentGroupSwitch.IsChecked == true;
            Settings.UseCurrentGroup = useCurrent;
            ManualGroupPanel.IsVisible = !useCurrent;
        };

        GroupIndexSpin.ValueChanged += (_, e) =>
            Settings.GroupIndex = (int)Math.Round(e.NewValue ?? 1);

        DutyTypeCombo.SelectionChanged += (_, _) =>
            Settings.DutyTypeName = DutyTypeCombo.SelectedItem is string s && s != "（全部）" ? s : "";

        MaskTemplateBox.TextChanged += (_, _) => Settings.MaskTemplate = MaskTemplateBox.Text ?? "";
        ContentTemplateBox.TextChanged += (_, _) => Settings.ContentTemplate = ContentTemplateBox.Text ?? "";
        SpeechSwitch.IsCheckedChanged += (_, _) => Settings.IsSpeechEnabled = SpeechSwitch.IsChecked == true;

        MaskDurationSpin.ValueChanged += (_, e) =>
            Settings.MaskDurationSeconds = (double)(e.NewValue ?? 4m);
        ContentDurationSpin.ValueChanged += (_, e) =>
            Settings.ContentDurationSeconds = (double)(e.NewValue ?? 8m);
    }

    /// <summary>
    /// 挂载到视觉树时 SettingsInternal 已被宿主赋值，此时才能安全回填。
    /// 构造函数里访问 Settings 会抛「过早访问行动项设置」。
    /// </summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_isLoaded) return;
        _isLoaded = true;

        var current = string.IsNullOrWhiteSpace(Settings.DutyTypeName)
            ? "（全部）"
            : Settings.DutyTypeName;
        DutyTypeCombo.SelectedItem = DutyTypeCombo.Items?.Contains(current) == true ? current : "（全部）";

        UseCurrentGroupSwitch.IsChecked = Settings.UseCurrentGroup;
        GroupIndexSpin.Value = Settings.GroupIndex;
        MaskTemplateBox.Text = Settings.MaskTemplate;
        ContentTemplateBox.Text = Settings.ContentTemplate;
        SpeechSwitch.IsChecked = Settings.IsSpeechEnabled;
        MaskDurationSpin.Value = (decimal)Settings.MaskDurationSeconds;
        ContentDurationSpin.Value = (decimal)Settings.ContentDurationSeconds;
        ManualGroupPanel.IsVisible = !Settings.UseCurrentGroup;
    }
}
