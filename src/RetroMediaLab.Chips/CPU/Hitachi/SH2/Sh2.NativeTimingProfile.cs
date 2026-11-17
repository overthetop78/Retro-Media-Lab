namespace RetroMediaLab.Chips.CPU.Hitachi.SH2;

public sealed record Sh2NativeCallbackTiming(string Category, ulong TscTicks, ulong Calls);
public sealed record Sh2NativeTimingProfile(ulong PayloadTscTicks, ulong CallbackTscTicks,
    ulong PayloadCalls, ulong CallbackCalls, IReadOnlyList<Sh2NativeCallbackTiming> Categories)
{
    public ulong CachedReadHits { get; init; }
    public ulong MappedReadHits { get; init; }
}

internal static class Sh2NativeTimingLayout
{
    internal const int PayloadStart = 0, CallbackStart = 1, PayloadTicks = 2, CallbackTicks = 3,
        PayloadCalls = 4, CallbackCalls = 5, Categories = 6, CachedReadHits = 16, MappedReadHits = 17, Words = 18;
    internal static int Category(uint kind) => kind switch
    {
        1 or 2 or 4 => 0, 0x101 or 0x102 or 0x104 => 1,
        0x200 or 0x210 or 0x211 => 2,
        >= 0x300 and <= 0x30f or 0x400ffe or 0x400ffd => 3, _ => 4
    };
}

public partial class Sh2<TBus> where TBus : Contracts.ISh2Bus
{
    private bool _useNativeTimingProfile;
    private readonly ulong[] _nativeTimingTotals = new ulong[Sh2NativeTimingLayout.Words];
    /// <summary>Diagnostic only; select on the owning thread outside Execute. Disabled emits no timestamp instructions.</summary>
    public bool UseNativeTimingProfile
    {
        get => _useNativeTimingProfile;
        set { if (value == _useNativeTimingProfile) return; ClearCodeCache(); _useNativeTimingProfile = value; ResetNativeTimingProfile(); }
    }
    public void ResetNativeTimingProfile() => Array.Clear(_nativeTimingTotals);
    public Sh2NativeTimingProfile GetNativeTimingProfile()
    {
        string[] names = ["read", "write", "clock", "idle", "other"];
        return new(_nativeTimingTotals[2], _nativeTimingTotals[3], _nativeTimingTotals[4], _nativeTimingTotals[5],
            names.Select((name, i) => new Sh2NativeCallbackTiming(name, _nativeTimingTotals[6 + i * 2], _nativeTimingTotals[7 + i * 2])).ToArray())
            {
                CachedReadHits = _nativeTimingTotals[Sh2NativeTimingLayout.CachedReadHits],
                MappedReadHits = _nativeTimingTotals[Sh2NativeTimingLayout.MappedReadHits]
            };
    }
    private void CollectNativeTiming(ReadOnlySpan<ulong> timing)
    {
        for (int i = Sh2NativeTimingLayout.PayloadTicks; i < timing.Length; i++) _nativeTimingTotals[i] += timing[i];
    }
}
