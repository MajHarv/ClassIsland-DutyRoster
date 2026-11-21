using System.Text.Json;
using System.Text.Json.Serialization;
using DutyRoster.Models;

namespace DutyRoster.Services;

/// <summary>
/// 配置持久化。不用 ClassIsland 的 ConfigureFileHelper，因为需要显式修复
/// 反序列化后为 null 的集合属性，否则设置页首次打开会崩溃。
/// </summary>
public sealed class ConfigService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _configPath;
    private readonly Action<string, Exception>? _logError;

    /// <summary>UTF-8（无 BOM），中文必须显式指定，否则在 GBK 系统上会乱码。</summary>
    private static readonly System.Text.Encoding Utf8NoBom = new System.Text.UTF8Encoding(false);

    public DutyConfig Config { get; private set; } = new();

    public string ConfigPath => _configPath;

    public ConfigService(string configFolder, Action<string, Exception>? logError = null)
    {
        _configPath = Path.Combine(configFolder, "duty_roster.json");
        _logError = logError;
    }

    /// <summary>加载配置，文件缺失或损坏时写入预置的空白值日表。</summary>
    public DutyConfig Load()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);

            if (!File.Exists(_configPath))
            {
                Config = DefaultDutyData.Create();
                Save();
                return Config;
            }

            // 显式指定 UTF-8：不能依赖平台默认编码（中文系统 ANSI 为 GBK，会导致中文乱码）
            var json = File.ReadAllText(_configPath, Utf8NoBom);
            var loaded = JsonSerializer.Deserialize<DutyConfig>(json, Options);
            if (loaded is null)
            {
                Config = DefaultDutyData.Create();
                Save();
                return Config;
            }

            Repair(loaded);
            Config = loaded;
            return Config;
        }
        catch (Exception ex)
        {
            _logError?.Invoke("加载值日表配置失败，已回退到空白值日表", ex);
            TryBackupCorrupted();
            Config = DefaultDutyData.Create();
            return Config;
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);

            // 先写临时文件再替换，避免中途退出留下半截配置
            var temp = _configPath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(Config, Options), Utf8NoBom);
            File.Move(temp, _configPath, true);
        }
        catch (Exception ex)
        {
            _logError?.Invoke("保存值日表配置失败", ex);
        }
    }

    public void ResetToDefault()
    {
        Config = DefaultDutyData.Create();
        Save();
    }

    private static void Repair(DutyConfig config)
    {
        config.Types ??= new();
        config.Groups ??= new();
        config.Schedules ??= new();
        config.Students ??= new();
        config.Rotation ??= new();
        config.Rotation.SkipDays ??= new();
        config.Rotation.ExtraSkipDates ??= new();

        if (string.IsNullOrWhiteSpace(config.Name)) config.Name = "值日表";

        foreach (var group in config.Groups)
        {
            group.Assignments ??= new();
            group.Name ??= "";
            foreach (var assignment in group.Assignments)
            {
                assignment.People ??= new();
                assignment.TypeName ??= "";
            }
        }

        foreach (var type in config.Types)
        {
            type.Name ??= "";
            if (string.IsNullOrWhiteSpace(type.Color)) type.Color = "#00BFFF";
        }

        if (config.Types.Count == 0 || config.Groups.Count == 0)
        {
            var seed = DefaultDutyData.Create();
            if (config.Types.Count == 0) config.Types = seed.Types;
            if (config.Groups.Count == 0) config.Groups = seed.Groups;
        }
    }

    private void TryBackupCorrupted()
    {
        try
        {
            if (!File.Exists(_configPath)) return;
            File.Copy(_configPath, _configPath + ".corrupted.bak", true);
        }
        catch
        {
            // 备份失败不应影响启动
        }
    }
}
