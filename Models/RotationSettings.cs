using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace DutyRoster.Models;

/// <summary>轮换设置。</summary>
public class RotationSettings : INotifyPropertyChanged
{
    private string _presetKind = Rotation.RotationKinds.Default;
    private DateTime _startDate = DateTime.Today;
    private ObservableCollectionEx<DayOfWeek> _skipDays = new();
    private ObservableCollectionEx<DateTime> _extraSkipDates = new();
    private int _sequenceLength;
    private int _biweeklyHalfLength;
    private int _manualGroupOffset;

    public string PresetKind
    {
        get => _presetKind;
        set => SetField(ref _presetKind, string.IsNullOrWhiteSpace(value) ? Rotation.RotationKinds.Default : value);
    }

    public DateTime StartDate
    {
        get => _startDate;
        set => SetField(ref _startDate, value.Date);
    }

    public ObservableCollectionEx<DayOfWeek> SkipDays
    {
        get => _skipDays;
        set => SetField(ref _skipDays, value ?? new());
    }

    public ObservableCollectionEx<DateTime> ExtraSkipDates
    {
        get => _extraSkipDates;
        set => SetField(ref _extraSkipDates, value ?? new());
    }

    public int SequenceLength
    {
        get => _sequenceLength;
        set => SetField(ref _sequenceLength, Math.Max(0, value));
    }

    /// <summary>双周轮换时每半周期的组数。</summary>
    public int BiweeklyHalfLength
    {
        get => _biweeklyHalfLength;
        set => SetField(ref _biweeklyHalfLength, Math.Max(0, value));
    }

    /// <summary>
    /// 手动组偏移。非 0 时，把「当前实际轮到第几组」整体平移该值，
    /// 用于手动校准「现在是第 X 组」。正数表示向后推，负数向前推。
    /// </summary>
    public int ManualGroupOffset
    {
        get => _manualGroupOffset;
        set => SetField(ref _manualGroupOffset, value);
    }

    /// <summary>
    /// 手动设置「当前为第 X 组」。内部换算为组偏移量，使今天的
    /// ResolveGroupIndex 恰好等于 groupNumber-1。
    /// </summary>
    public void SetCurrentGroupNumber(int groupNumber, int groupCount)
    {
        if (groupCount <= 0) return;
        var desiredIndex = Math.Max(0, groupNumber - 1) % groupCount;

        // 计算今天在「无偏移」时的自动索引
        var preset = Preset;
        var baseCtx = Rotation.RotationContext.Create(this, groupCount);
        var autoIndex = preset.ResolveGroupIndex(baseCtx, DateTime.Today);
        if (autoIndex < 0) autoIndex = 0;

        var delta = desiredIndex - autoIndex;
        var n = groupCount;
        // 归一化到 [-n/2, n/2) 区间，取最短路径
        delta = ((delta % n) + n) % n;
        if (delta > n / 2) delta -= n;

        ManualGroupOffset = delta;
    }

    /// <summary>构造给轮换算法使用的只读上下文。</summary>
    public Rotation.RotationContext BuildContext(int groupCount)
    {
        var seq = PresetKind == Rotation.RotationKinds.Biweekly && BiweeklyHalfLength > 0
            ? BiweeklyHalfLength
            : SequenceLength;

        var baseContext = Rotation.RotationContext.Create(this, groupCount);
        return new Rotation.RotationContext
        {
            StartDate = baseContext.StartDate,
            GroupCount = baseContext.GroupCount,
            SkipDays = baseContext.SkipDays,
            ExtraSkipDates = baseContext.ExtraSkipDates,
            SequenceLength = seq,
            ManualGroupOffset = ManualGroupOffset
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    [JsonIgnore]
    public Rotation.IRotationPreset Preset => Rotation.RotationPresetRegistry.Resolve(_presetKind);
}

/// <summary>可序列化的 ObservableCollection。</summary>
public class ObservableCollectionEx<T> : System.Collections.ObjectModel.ObservableCollection<T>
{
    public ObservableCollectionEx() { }
    public ObservableCollectionEx(IEnumerable<T> items) : base(items) { }
}
