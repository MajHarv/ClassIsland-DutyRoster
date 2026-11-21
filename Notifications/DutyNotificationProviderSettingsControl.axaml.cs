using Avalonia;
using ClassIsland.Core.Abstractions.Controls;
using DutyRoster.Models;

namespace DutyRoster.Notifications;

/// <summary>
/// 值日提醒提供方的设置界面。ClassIsland 会把它嵌在
/// 应用设置 → 提醒 → 值日生提醒 里，与其他原生提醒提供方并列。
/// </summary>
public partial class DutyNotificationProviderSettingsControl : NotificationProviderControlBase<DutyNotificationSettings>
{
    private bool _isLoaded;

    public DutyNotificationProviderSettingsControl()
    {
        InitializeComponent();

        EnabledSwitch.IsCheckedChanged += (_, _) => Settings.IsEnabled = EnabledSwitch.IsChecked == true;
        LeadSecondsSpin.ValueChanged += (_, e) =>
            Settings.LeadSeconds = (int)Math.Round(e.NewValue ?? 0);
        MaskTemplateBox.TextChanged += (_, _) => Settings.MaskTemplate = MaskTemplateBox.Text ?? "";
        ContentTemplateBox.TextChanged += (_, _) => Settings.ContentTemplate = ContentTemplateBox.Text ?? "";
        SpeechSwitch.IsCheckedChanged += (_, _) => Settings.IsSpeechEnabled = SpeechSwitch.IsChecked == true;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_isLoaded) return;
        _isLoaded = true;

        EnabledSwitch.IsChecked = Settings.IsEnabled;
        LeadSecondsSpin.Value = Settings.LeadSeconds;
        MaskTemplateBox.Text = Settings.MaskTemplate;
        ContentTemplateBox.Text = Settings.ContentTemplate;
        SpeechSwitch.IsChecked = Settings.IsSpeechEnabled;
    }
}
