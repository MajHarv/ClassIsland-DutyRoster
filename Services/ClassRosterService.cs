using DutyRoster.Models;

namespace DutyRoster.Services;

/// <summary>班级名单服务。随机点名可用，随机排班为预留接口。</summary>
public interface IClassRosterService
{
    IReadOnlyList<Student> Students { get; }

    /// <summary>参与随机抽取的学生（排除已标记不参与者）。</summary>
    IReadOnlyList<Student> EligibleStudents { get; }

    /// <summary>每行一位同学，可带学号（「01 张三」）。</summary>
    void ImportFromText(string text, bool append);

    string ExportToText();

    void Clear();

    /// <summary>随机点名；avoidRecent 时尽量避开近期已点到的人。</summary>
    IReadOnlyList<Student> PickRandomStudents(int count, bool avoidRecent = true);

    /// <summary>随机排值日表 —— 预留接口，算法尚未实现。</summary>
    RandomRosterResult GenerateRandomRoster(RandomRosterRequest request);
}

public sealed record RandomRosterRequest(
    int GroupCount,
    IReadOnlyList<DutyType> DutyTypes,
    bool KeepExisting = false);

public sealed record RandomRosterResult(
    bool Success,
    string Message,
    Dictionary<string, Dictionary<string, List<string>>> Assignments)
{
    public static RandomRosterResult Fail(string message) => new(false, message, new());
}

public sealed class ClassRosterService : IClassRosterService
{
    private readonly List<Student> _students = new();
    private readonly List<string> _recentPicks = new();
    private readonly Random _random = new();

    private const int RecentPickLimit = 12;

    public IReadOnlyList<Student> Students => _students;

    public IReadOnlyList<Student> EligibleStudents =>
        _students.Where(s => !s.IsExcluded && !string.IsNullOrWhiteSpace(s.Name)).ToList();

    public void ImportFromText(string text, bool append)
    {
        if (!append) _students.Clear();
        if (string.IsNullOrWhiteSpace(text)) return;

        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim().TrimEnd('\r');
            if (line.Length == 0) continue;

            var (number, name) = SplitLine(line);
            if (name.Length == 0) continue;
            _students.Add(new Student { Number = number, Name = name });
        }
    }

    public string ExportToText()
        => string.Join(Environment.NewLine, _students.Select(s =>
            string.IsNullOrWhiteSpace(s.Number) ? s.Name : $"{s.Number} {s.Name}"));

    public void Clear()
    {
        _students.Clear();
        _recentPicks.Clear();
    }

    public IReadOnlyList<Student> PickRandomStudents(int count, bool avoidRecent = true)
    {
        var pool = EligibleStudents.ToList();
        if (pool.Count == 0 || count <= 0) return Array.Empty<Student>();

        // 近期未被点到的人优先；只有「新鲜池」能覆盖这次请求时才限缩，
        // 否则回退到全量池，避免请求超出新鲜人数时只返回少数人。
        if (avoidRecent)
        {
            var fresh = pool.Where(s => !_recentPicks.Contains(s.Name)).ToList();
            if (fresh.Count >= count) pool = fresh;
        }

        var picked = new List<Student>();
        for (var i = 0; i < count && pool.Count > 0; i++)
        {
            var index = _random.Next(pool.Count);
            var student = pool[index];
            pool.RemoveAt(index);
            picked.Add(student);
            RememberPick(student.Name);
        }

        return picked;
    }

    public RandomRosterResult GenerateRandomRoster(RandomRosterRequest request)
    {
        // 预留接口：按各类型 RequiredPeople 之和轮流发牌，保证每人只出现一次
        return RandomRosterResult.Fail(
            "随机排值日表功能尚未实现。当前版本请手动填写或在设置页导入值日表。");
    }

    private void RememberPick(string name)
    {
        _recentPicks.Remove(name);
        _recentPicks.Add(name);
        while (_recentPicks.Count > RecentPickLimit) _recentPicks.RemoveAt(0);
    }

    private static (string Number, string Name) SplitLine(string line)
    {
        var separators = new[] { ' ', '、', ',', '\t', '，' };
        var idx = line.IndexOfAny(separators);
        if (idx <= 0) return ("", line.Trim());

        var head = line[..idx].Trim();
        var tail = line[(idx + 1)..].Trim();
        if (tail.Length == 0) return ("", head);

        return head.All(char.IsDigit) ? (head, tail) : ("", line.Trim());
    }
}
