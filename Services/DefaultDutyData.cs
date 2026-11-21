using DutyRoster.Models;

namespace DutyRoster.Services;

/// <summary>首次启动写入的值日表结构：预置组别、值日类型与时段，人员留空由用户填写。</summary>
public static class DefaultDutyData
{
    private static readonly (string Name, string Color, string Icon, int People)[] TypeSeed =
    [
        ("扫地",   "#4FC3F7", "🧹", 2),
        ("拖地",   "#81D4FA", "🧽", 2),
        ("擦黑板", "#FFF176", "🖊", 1),
        ("排桌椅", "#FFB74D", "🪑", 2),
        ("包干区", "#AED581", "🌳", 1)
    ];

    private static readonly string[] GroupSeed =
    [
        "第一组", "第二组", "第三组", "第四组", "第五组", "第六组"
    ];

    public static DutyConfig Create()
    {
        var config = new DutyConfig
        {
            Name = "值日生表",
            ConfigVersion = 1,
            IsEnabled = true
        };

        foreach (var (name, color, icon, people) in TypeSeed)
        {
            config.Types.Add(new DutyType
            {
                Name = name,
                Color = color,
                Icon = icon,
                RequiredPeople = people,
                IsEnabled = true
            });
        }

        // 只建表格结构，人员格留空
        foreach (var groupName in GroupSeed)
        {
            var group = new DutyGroup { Name = groupName, IsEnabled = true };
            foreach (var type in TypeSeed)
                group.Assignments.Add(new GroupAssignment { TypeName = type.Name });
            config.Groups.Add(group);
        }

        // 按周轮换，从本周一开始，跳过周末
        var today = DateTime.Today;
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        config.Rotation.PresetKind = Models.Rotation.RotationKinds.Weekly;
        config.Rotation.StartDate = monday;
        config.Rotation.SkipDays.Add(DayOfWeek.Saturday);
        config.Rotation.SkipDays.Add(DayOfWeek.Sunday);

        config.Schedules.Add(new DutySchedule
        {
            Name = "早读前",
            Start = new TimeSpan(7, 30, 0),
            End = new TimeSpan(8, 0, 0)
        });
        config.Schedules.Add(new DutySchedule
        {
            Name = "放学前",
            Start = new TimeSpan(17, 20, 0),
            End = new TimeSpan(17, 50, 0)
        });

        return config;
    }
}
