namespace RetroMediaLab.Chips.CPU.Hitachi.SH2;

public sealed record Sh2CompiledAddressProfile(uint Address, long Builds);
public sealed record Sh2CompiledBodyProfile(string Words, string FetchWaits, long Builds);
public sealed record Sh2CompilationProfile(long Attempts, int UniqueAddresses, long RecompiledAfterEviction,
    long Evictions, long VariantEvictions, long CacheClears, long SameBodyReuses, long VariantReuses,
    long CodeChanges, long TimingChanges, long UnmappedChanges, int ActiveBlocks, int InactiveBlocks,
    long Instructions, long IlBytes, int MaximumIlBytes, long ScanTicks, long PrefixTicks, long EmitTicks,
    long DelegateTicks, long PrepareTicks, long PublishTicks, long TotalTicks, Sh2CompiledAddressProfile[] Addresses,
    int UniqueInstructionSequences, int UniqueTimingSequences, Sh2CompiledBodyProfile[] Bodies);

public partial class Sh2<TBus> where TBus : Contracts.ISh2Bus
{
    private CompilationTiming? _compilationTiming;
    private sealed class CompilationTiming
    {
        internal readonly Dictionary<uint, long> Builds = [];
        internal readonly HashSet<uint> Evicted = [];
        internal readonly HashSet<string> InstructionSequences = [];
        internal readonly Dictionary<(string Words, string Waits), long> Bodies = [];
        internal long Attempts, RecompiledAfterEviction, Evictions, VariantEvictions, CacheClears;
        internal long SameBodyReuses, VariantReuses, CodeChanges, TimingChanges, UnmappedChanges;
        internal long Instructions, IlBytes, Scan, Prefix, Emit, Delegate, Prepare, Publish, Total;
        internal int MaximumIlBytes;
    }
    /// <summary>Call on the owning emulation thread, outside an execution slice.</summary>
    public void EnableCompilationProfiling(bool enabled) => _compilationTiming = enabled ? new() : null;
    public Sh2CompilationProfile GetCompilationProfile()
    {
        var p = _compilationTiming;
        return new(p?.Attempts ?? 0, p?.Builds.Count ?? 0, p?.RecompiledAfterEviction ?? 0,
            p?.Evictions ?? 0, p?.VariantEvictions ?? 0, p?.CacheClears ?? 0, p?.SameBodyReuses ?? 0, p?.VariantReuses ?? 0,
            p?.CodeChanges ?? 0, p?.TimingChanges ?? 0, p?.UnmappedChanges ?? 0, _codeBlocks.Count, _inactiveCodeBlocks.Values.Sum(x => x.Count),
            p?.Instructions ?? 0, p?.IlBytes ?? 0, p?.MaximumIlBytes ?? 0, p?.Scan ?? 0, p?.Prefix ?? 0, p?.Emit ?? 0,
            p?.Delegate ?? 0, p?.Prepare ?? 0, p?.Publish ?? 0, p?.Total ?? 0,
            p?.Builds.OrderByDescending(x => x.Value).ThenBy(x => x.Key).Take(32).Select(x => new Sh2CompiledAddressProfile(x.Key, x.Value)).ToArray() ?? [],
            p?.InstructionSequences.Count ?? 0, p?.Bodies.Count ?? 0,
            p?.Bodies.OrderByDescending(x => x.Value).Take(32).Select(x => new Sh2CompiledBodyProfile(x.Key.Words, x.Key.Waits, x.Value)).ToArray() ?? []);
    }
    private void ProfileInstructionSequence(List<ushort> words, List<int> waits)
    {
        if (_compilationTiming is not { } p) return;
        string code = string.Join(',', words.Select(x => x.ToString("X4")));
        var key = (code, string.Join(',', waits));
        p.InstructionSequences.Add(code);
        p.Bodies[key] = p.Bodies.GetValueOrDefault(key) + 1;
    }
    private void ProfileRebuild(CodeBlock block, ushort first)
    {
        var p = _compilationTiming;
        if (p is null) return;
        if (block.Instructions[0] != first) { p.CodeChanges++; return; }
        for (int i = 1; i < block.Instructions.Length; i++)
        {
            if (!TryReadMappedCode(block.Address + (uint)(i * 2), out ushort word, out int wait)) { p.UnmappedChanges++; return; }
            if (word != block.Instructions[i]) { p.CodeChanges++; return; }
            if (wait != block.FetchWaits[i]) { p.TimingChanges++; return; }
        }
    }
}
