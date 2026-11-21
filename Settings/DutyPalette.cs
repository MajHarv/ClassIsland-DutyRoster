using Avalonia.Media;

namespace DutyRoster.Settings;

/// <summary>值日类型可选颜色（参考节日配色风格的明亮柔和色系）。</summary>
public static class DutyPalette
{
    /// <summary>可选的强调色。</summary>
    public static readonly string[] Palette =
    [
        "#F5B301", // 黄
        "#4CAF50", // 绿
        "#2E7D32", // 深绿
        "#F9A825", // 橙黄
        "#E53935", // 红
        "#7B1FA2", // 紫
        "#E91E63", // 玫红
        "#1E88E5", // 蓝
        "#26A69A", // 青
        "#FB8C00", // 橙
        "#5D4037", // 棕
        "#546E7A"  // 灰蓝
    ];

    /// <summary>返回与给定索引对应的颜色，越界时循环。</summary>
    public static string At(int index)
        => Palette[((index % Palette.Length) + Palette.Length) % Palette.Length];

    public static Color Parse(string? hex, Color fallback)
        => string.IsNullOrWhiteSpace(hex) || !Color.TryParse(hex, out var c) ? fallback : c;

    /// <summary>取「理想」前景色：亮色背景用深字，深色背景用白字。</summary>
    public static IBrush ForegroundFor(string? hex)
    {
        var c = Parse(hex, Color.Parse("#4CAF50"));
        var luminance = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
        return new SolidColorBrush(luminance > 186 ? Color.Parse("#1A1A1A") : Avalonia.Media.Colors.White);
    }
}
