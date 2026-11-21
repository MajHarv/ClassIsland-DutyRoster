using System.IO;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using DutyRoster.Automation;
using DutyRoster.Components;
using DutyRoster.Notifications;
using DutyRoster.Services;
using DutyRoster.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DutyRoster;

/// <summary>插件入口。</summary>
[PluginEntrance]
public class Plugin : PluginBase
{
    /// <summary>全局配置服务，在 Initialize 中同步创建，保证任何控件构造时已就绪。</summary>
    public static ConfigService ConfigService { get; private set; } = null!;

    public static Models.DutyConfig Config => ConfigService.Config;

    public static IClassRosterService Roster { get; } = new ClassRosterService();

    public static event EventHandler? ConfigChanged;

    public static string ConfigFolder { get; private set; } = "";

    /// <summary>提醒提供方实例，供自动化 Action 复用其 public ShowNotification。</summary>
    internal static Notifications.DutyNotificationProvider? NotificationProvider { get; private set; }

    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        ConfigFolder = PluginConfigFolder;

        ConfigService = new ConfigService(PluginConfigFolder, LogError);
        ConfigService.Load();

        services.AddSingleton(ConfigService);
        services.AddSettingsPage<DutySettingsPage>();
        services.AddComponent<DutyDisplayComponent, Components.DutyDisplayComponentSettingsControl>();
        services.AddNotificationProvider<DutyNotificationProvider>();
        services.AddTrigger<DutyTimeStartTrigger, DutyTimeStartTriggerSettingsControl>();
        services.AddAction<Automation.DutyRemindAction, Automation.DutyRemindActionSettingsControl>();
    }

    public static void SaveAndNotify()
    {
        ConfigService?.Save();
        ConfigChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void NotifyChanged() => ConfigChanged?.Invoke(null, EventArgs.Empty);

    public static void ResetToDefault()
    {
        ConfigService.ResetToDefault();
        ConfigChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// 通过提醒提供方实例显示一条提醒。自动化 Action 调用此方法，
    /// 避免直接依赖 internal 的 NotificationHostService API。
    /// </summary>
    public static void ShowDutyNotification(
        ClassIsland.Core.Models.Notification.NotificationContent mask,
        ClassIsland.Core.Models.Notification.NotificationContent overlay)
    {
        NotificationProvider?.ShowNotification(new ClassIsland.Core.Models.Notification.NotificationRequest
        {
            MaskContent = mask,
            OverlayContent = overlay
        });
    }

    /// <summary>由提醒提供方在构造时调用，登记自身以便 Action 复用。</summary>
    internal static void RegisterNotificationProvider(Notifications.DutyNotificationProvider provider)
        => NotificationProvider = provider;

    private static void LogError(string message, Exception ex)
    {
        try
        {
            if (string.IsNullOrEmpty(ConfigFolder)) return;
            Directory.CreateDirectory(ConfigFolder);
            File.AppendAllText(
                Path.Combine(ConfigFolder, "errors.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // 日志写入失败不应影响插件运行
        }
    }
}
