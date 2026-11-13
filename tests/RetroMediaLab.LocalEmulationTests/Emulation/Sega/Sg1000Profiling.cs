using System.IO;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Sega.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services;

namespace RetroMediaLab.Tests.Emulation.Sega;

/// <summary>Reusable manual profiling tool. Discovery never opens a ROM or starts diagnostics.
/// Profile-Sg1000.ps1 creates and removes the external-file xUnit entry point for each invocation.</summary>
internal static class Sg1000Profiling
{
    internal sealed record Request(string[] Games, string OutputRoot, string DiagnosticClientPath,
        string TraceToolPath, string Mode = "Both", int Frames = 120, int WarmupFrames = 2,
        string Layout = "Auto", string? CompareTo = null, int Repetitions = 1);
    internal sealed record FrameRow(int Frame, string Phase, long MachineNanoseconds, double WallMs,
        double ProcessCpuMs, long AllocatedBytes, double GcPauseMs, int Gc0, int Gc1, int Gc2,
        ushort Pc, long Instructions, long CpuClocks, long MasterHalfEdges, int KnownPixels,
        int ColourMask, bool VdpQualified, bool CpuFaulted, bool IntAsserted, bool NmiPending,
        int AudioFrames, int AudioPeak, int NonzeroSamples, string VideoHash, string PcmHash,
        long BusRangeDeliveries = 0, long CpuOutputWords = 0, long LogicInputWords = 0, long MemoryInputWords = 0,
        long VramReadWaveDeliveries = 0, long DramExplicitPinDeliveries = 0, long VideoSynchronizationCalls = 0,
        long NativeVideoSamples = 0, long CpuBusResolutions = 0, long HeldWaitSamples = 0, long HeldControlSamples = 0,
        long DirectCauseVisits = 0, long GateCauseVisits = 0, long GateSkippedCpuWords = 0, long MemoryResponseRecordsOmitted = 0,
        double BoardWallMs = 0, double PresentationWallMs = 0, double AudioWallMs = 0,
        long DramOutputNotifications = 0, long VramDataDeliveries = 0, long VramDataGroups = 0,
        long DramReadOutputPrograms = 0, long VramReadProgramWords = 0, long DramReadOutputFallbacks = 0);
    internal sealed record Statistics(double MedianMs, double MeanMs, double P95Ms, double MinMs,
        double MaxMs, double HostFps, double NominalPercent, long AllocatedBytes, double GcPauseMs);
    internal sealed record Cost(string Name, double OwnMs, double InclusiveMs, double OwnPercent, double InclusivePercent);
    internal sealed record CpuProfile(double ScopedSampleMs, double BlockedSampleMs,
        IReadOnlyList<Cost> Methods, IReadOnlyDictionary<string, double> OwnGroupsMs);
    internal sealed record Repeatability(int Repetitions, double[] RunMeansMs, double[] RunMediansMs,
        double MeanMs, double MinimumRunMeanMs, double MaximumRunMeanMs,
        double StandardDeviationMs, double CoefficientVariationPercent, double MedianAbsoluteDeviationMs);
    internal sealed record OutputInfo(double NativeFps, long? ProcessorHertz, int Width, int Height,
        int Pitch, string PixelFormat, float AspectRatio, int SampleRate, int AudioChannels);
    internal sealed record Pass(string Kind, string Directory, double InitializeMs, string CoreSha256,
        bool JitOptimizationsDisabled, Statistics Statistics, FrameRow[] Frames, string StateHash,
        string FinalVideoHash, string AudioHash, CpuProfile? Cpu, OutputInfo Output,
        string BinaryFingerprint = "", IReadOnlyDictionary<string,string>? BinaryHashes = null);
    internal sealed record Comparison(string PreviousReport, double PreviousMedianMs, double CurrentMedianMs,
        double TimeReductionPercent, bool FinalStateEqual, bool VideoEqual, bool AudioEqual,
        int ComparedFrames = 0, bool EveryFrameEqual = false,
        string PerformanceAssessment = "DescriptiveOnly", double? PreviousRepeatedMeanMs = null,
        double? CurrentRepeatedMeanMs = null, double? RepeatedMeanTimeDifferencePercent = null);
    internal sealed record Result(string Rom, long Bytes, string Sha256, string Layout, int FrameCount,
        int WarmupFrames, string Runtime, string Status, string Directory, Pass[] Passes,
        Comparison? Comparison, string? Error, string Host = "", int ScenarioVersion = 1, string Execution = "Reference",
        Repeatability? Repeatability = null, string TimingPolicy = "default-tiering");
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    internal static async Task<Result[]> RunRequestedAsync(string requestPath, Action<string>? log = null)
    {
        var request = JsonSerializer.Deserialize<Request>(await File.ReadAllTextAsync(requestPath).ConfigureAwait(false), Json)
            ?? throw new InvalidDataException("Missing profiling request.");
        if (request.Frames < 1 || request.WarmupFrames < 0 || request.Repetitions is < 1 or > 100 || request.Games is not { Length: > 0 }
            || request.Mode is not ("Profile" or "Measure" or "Both")) throw new ArgumentException("Invalid profiling request.");
        var results = new List<Result>();
        foreach (string game in request.Games)
        {
            string name = Path.GetFileNameWithoutExtension(game);
            foreach (char invalid in Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '_');
            string gameDirectory = Path.Combine(request.OutputRoot, name);
            string directory = Path.Combine(gameDirectory, DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N")[..6]);
            Directory.CreateDirectory(directory);
            byte[] rom = []; string hash = string.Empty, layout = request.Layout;
            var passes = new List<Pass>(); string? error = null;
            try
            {
                rom = await File.ReadAllBytesAsync(game).ConfigureAwait(false); hash = Hash(rom);
                layout = ResolveLayout(hash, request.Layout);
                log?.Invoke($"{Path.GetFileName(game)}: {rom.Length} bytes, {layout}, {request.Mode}, {request.Frames} native frames");
                if (request.Mode != "Profile" && request.Repetitions > 1)
                    passes.Add(await RunPass(request, rom, layout, directory, "Prime", log).ConfigureAwait(false));
                if (request.Mode != "Profile")
                    for (int repetition = 0; repetition < request.Repetitions; repetition++)
                        passes.Add(await RunPass(request, rom, layout, directory,
                            repetition == 0 ? "Measure" : $"Measure-{repetition + 1:D2}", log).ConfigureAwait(false));
                if (request.Mode != "Measure")
                    passes.Add(await RunPass(request, rom, layout, directory, "Profile", log).ConfigureAwait(false));
                if (passes.Skip(1).Any(pass => passes[0].StateHash != pass.StateHash
                    || pass.Frames.Length != passes[0].Frames.Length
                    || !passes[0].Frames.Zip(pass.Frames).All(pair => SameNativeFrame(pair.First,pair.Second))))
                    throw new InvalidOperationException("A repetition or tracing changed a native state, frame observation or PCM block.");
            }
            catch (Exception failure) { error = failure.ToString(); log?.Invoke($"{Path.GetFileName(game)}: FAILED: {failure.Message}"); }
            var result = new Result(game, rom.Length, hash, layout, request.Frames, request.WarmupFrames,
                System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                error is null ? "Completed" : "Failed", directory, passes.ToArray(), null, error,
                Environment.MachineName + "/" + Environment.ProcessorCount + "/" + System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture,
                Execution: "ReceivedSignalRanges",
                Repeatability: passes.Any(pass => pass.Kind.StartsWith("Measure", StringComparison.Ordinal))
                    ? SummarizeRepetitions(passes.Where(pass => pass.Kind.StartsWith("Measure", StringComparison.Ordinal)).Select(pass => pass.Statistics.MeanMs).ToArray(),
                        passes.Where(pass => pass.Kind.StartsWith("Measure", StringComparison.Ordinal)).Select(pass => pass.Statistics.MedianMs).ToArray()) : null,
                TimingPolicy: (Environment.GetEnvironmentVariable("DOTNET_TieredCompilation") == "0" ? "tiering-disabled" : "default-tiering")
                    + (passes.Any(pass => pass.Kind == "Prime") ? "/primed" : ""));
            result = result with { Comparison = FindComparison(request, result, gameDirectory) };
            if (result.Comparison is { } compared && (!compared.FinalStateEqual || !compared.VideoEqual
                || !compared.AudioEqual || !compared.EveryFrameEqual))
                result = result with { Status = "Failed", Error = "Current hardware observations differ from the retained reference: "
                    + $"state={compared.FinalStateEqual}, video={compared.VideoEqual}, PCM={compared.AudioEqual}, every frame={compared.EveryFrameEqual}." };
            await File.WriteAllTextAsync(Path.Combine(directory, "report.json"), JsonSerializer.Serialize(result, Json)).ConfigureAwait(false);
            await File.WriteAllTextAsync(Path.Combine(directory, "report.md"), Markdown(result)).ConfigureAwait(false);
            results.Add(result);
            await File.WriteAllTextAsync(Path.Combine(request.OutputRoot, "latest-results.json"), JsonSerializer.Serialize(results, Json)).ConfigureAwait(false);
            log?.Invoke($"Saved: {Path.Combine(directory, "report.md")}");
        }
        return results.ToArray();
    }

    private static string ResolveLayout(string hash, string requested)
    {
        if (requested != "Auto") return requested;
        // These content identities were read from the user's files and matched
        // to the physically documented cart boards. Names/lengths alone are not identities.
        return hash switch
        {
            "305ad1ed2aa7b692224b2a0dc5b582ae86e4798cf40c786c1ab73c48eff59b23" => "TheCastle",
            "ae20568a13010c3773928e5073fc25a52006ae0242263c7a31118a4d56fad8da" => "Othello",
            _ => "Standard"
        };
    }

    private static async Task<Pass> RunPass(Request request, byte[] rom, string layout, string directory,
        string kind, Action<string>? log)
    {
        string passDirectory = Path.Combine(directory, kind); Directory.CreateDirectory(passDirectory);
        EmulatorCore? core = null; SelfTrace? trace = null;
        using var process = Process.GetCurrentProcess();
        try
        {
            var initialize = Stopwatch.StartNew();
            core = new(); core.Initialize(new MachineConfiguration("Sg1000", "segaemu",
                Options: new Dictionary<string, string> { ["segaemu.cartridge.layout"] = layout }), rom);
            initialize.Stop();
            for (int frame = 0; frame < request.WarmupFrames; frame++) { core.RunFrame(); Drain(core, null); }
            core.CollectFrameTimings = kind == "Profile";
            if (kind == "Profile") trace = SelfTrace.Start(request.DiagnosticClientPath, Path.Combine(passDirectory, "cpu.nettrace"));
            var rows = new List<FrameRow>(request.Frames); var audio = new List<short>();
            using (var progress = new StreamWriter(Path.Combine(passDirectory, "frames.jsonl"), append: false))
            {
                for (int frame = 0; frame < request.Frames; frame++)
                {
                    int startEnd = Math.Max(1, request.Frames * 3 / 4), neutralEnd = Math.Max(1, request.Frames / 8);
                    bool pause = frame == request.Frames * 9 / 10;
                    uint buttons = pause ? 1U << 3 : frame < neutralEnd ? 0 : frame < startEnd ? 1U : (1U << 7) | (1U << 8);
                    string phase = pause ? "Pause" : frame < neutralEnd ? "Neutral" : frame < startEnd ? "Button1" : "DirectionAndButton2";
                    core.SetInput(EmulationInputSnapshot.Empty with
                    { Controllers = [EmulationControllerState.Empty with { Buttons = buttons }, EmulationControllerState.Empty] });
                    TimeSpan gcPause = GC.GetTotalPauseDuration(), cpuTime = process.TotalProcessorTime;
                    int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
                    long allocated = GC.GetAllocatedBytesForCurrentThread();
                    long start = Stopwatch.GetTimestamp(); core.RunFrame(); long end = Stopwatch.GetTimestamp();
                    // All counters are taken before observers/exporters allocate their own data.
                    long bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
                    double pauseMs = (GC.GetTotalPauseDuration() - gcPause).TotalMilliseconds;
                    double cpuMs = (process.TotalProcessorTime - cpuTime).TotalMilliseconds;
                    int c0 = GC.CollectionCount(0) - gen0, c1 = GC.CollectionCount(1) - gen1, c2 = GC.CollectionCount(2) - gen2;
                    var sample = Drain(core, audio); var board = core.Board;
                    int known = 0, colours = 0;
                    foreach (bool value in board.PixelKnowledge.Span) if (value) known++;
                    foreach (byte colour in board.ColourIndices.Span) colours |= 1 << colour;
                    var interrupts = board.Cpu.InterruptState;
                    var row = new FrameRow(frame, phase, board.TimeNanoseconds, Stopwatch.GetElapsedTime(start, end).TotalMilliseconds,
                        cpuMs, bytes, pauseMs, c0, c1, c2, board.Cpu.Registers.PC, board.Cpu.InstructionsCompleted,
                        board.Cpu.InputClockCount, board.Vdp.ClockState.HalfEdges, known, colours,
                        board.Vdp.IsCpuInterfaceQualified, board.Cpu.IsFaulted, interrupts.IntAsserted, interrupts.NmiPending,
                        sample.Frames, sample.Peak, sample.Nonzero, Hash(core.LatestVideoFrame!.Pixels.Span), sample.Hash,
                        board.BusRangeDeliveries, board.CpuOutputWords, board.LogicInputWords, board.MemoryInputWords,
                        board.VramReadWaveDeliveries, board.DramExplicitPinDeliveries, board.VideoSynchronizationCalls, board.NativeVideoSamples,
                        board.CpuBusResolutions, board.Cpu.BusRangeStatistics.HeldWaitSamples, board.Cpu.BusRangeStatistics.HeldControlSamples,
                        board.DirectCauseVisits, board.GateCauseVisits, board.GateSkippedCpuWords, board.MemoryResponseRecordsOmitted,
                        core.LastFrameTimings.BoardMs,core.LastFrameTimings.PresentationMs,core.LastFrameTimings.AudioMs,
                        board.DramOutputNotifications, board.VramDataDeliveries, board.VramDataGroups,
                        board.DramReadOutputPrograms, board.VramReadProgramWords, board.DramReadOutputFallbacks);
                    rows.Add(row); progress.WriteLine(JsonSerializer.Serialize(row)); progress.Flush();
                    if (frame % 15 == 0) log?.Invoke($"{kind}: frame {frame + 1}/{request.Frames}, {row.WallMs:F2}ms, PC={row.Pc:X4}, alloc={bytes}, GC={pauseMs:F3}ms");
                    if (board.Cpu.IsFaulted) throw new InvalidOperationException($"CPU fault at frame{frame},PC={row.Pc:X4}.");
                }
            }
            if (trace is not null) { await trace.DisposeAsync().ConfigureAwait(false); trace = null; }
            byte[] state = core.SaveState();
            await File.WriteAllBytesAsync(Path.Combine(passDirectory, "state.json"), state).ConfigureAwait(false);
            await File.WriteAllBytesAsync(Path.Combine(passDirectory, "frame.xrgb8888"), core.LatestVideoFrame!.Pixels.ToArray()).ConfigureAwait(false);
            WriteWav(Path.Combine(passDirectory, "audio.wav"), audio, core.SampleRate);
            CpuProfile? costs = null;
            if (kind == "Profile")
            {
                await ConvertTrace(request.TraceToolPath, passDirectory).ConfigureAwait(false);
                using var document = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(passDirectory, "cpu.speedscope.json")).ConfigureAwait(false));
                costs = AnalyzeCpu(document.RootElement);
                if (costs.ScopedSampleMs <= 0) throw new InvalidDataException("No native RunFrame samples were captured.");
                await File.WriteAllTextAsync(Path.Combine(passDirectory, "cpu-costs.json"), JsonSerializer.Serialize(costs, Json)).ConfigureAwait(false);
            }
            var attribute = typeof(EmulatorCore).Assembly.GetCustomAttribute<DebuggableAttribute>();
            var binaryHashes = new Dictionary<string,string>
            {
                ["Sega"] = Hash(File.ReadAllBytes(typeof(EmulatorCore).Assembly.Location)),
                ["Chips"] = Hash(File.ReadAllBytes(typeof(RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Services.CpuZ80).Assembly.Location)),
                ["Emulation"] = Hash(File.ReadAllBytes(typeof(MachineConfiguration).Assembly.Location))
            };
            string fingerprint = Hash(Encoding.UTF8.GetBytes(string.Join("|",binaryHashes.OrderBy(pair=>pair.Key)
                .Select(pair=>pair.Key+":"+pair.Value))));
            return new(kind, passDirectory, initialize.Elapsed.TotalMilliseconds, core.CoreSha256,
                attribute?.IsJITOptimizerDisabled == true, Summarize(rows, core.FramesPerSecond), rows.ToArray(), Hash(state),
                Hash(core.LatestVideoFrame.Pixels.Span), Hash(System.Runtime.InteropServices.MemoryMarshal.AsBytes(audio.ToArray().AsSpan())), costs,
                new(core.FramesPerSecond, core.ProcessorClockHz, core.LatestVideoFrame.Width, core.LatestVideoFrame.Height,
                    core.LatestVideoFrame.Pitch, core.LatestVideoFrame.PixelFormat.ToString(), core.LatestVideoFrame.AspectRatio,
                    core.SampleRate, core.LatestAudioChunk?.ChannelCount ?? 1),fingerprint,binaryHashes);
        }
        finally
        {
            try { if (trace is not null) await trace.DisposeAsync().ConfigureAwait(false); }
            finally { core?.Dispose(); }
        }
    }

    private static (int Frames, int Peak, int Nonzero, string Hash) Drain(EmulatorCore core, List<short>? destination)
    {
        int frames = 0, peak = 0, nonzero = 0;
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        while (core.TryDequeueAudio(out var chunk))
        {
            if (!chunk!.HasValidLength) throw new InvalidDataException("Invalid PCM chunk.");
            frames += chunk.FrameCount;
            digest.AppendData(System.Runtime.InteropServices.MemoryMarshal.AsBytes(chunk.InterleavedSamples.Span));
            foreach (short sample in chunk.InterleavedSamples.Span)
            { peak = Math.Max(peak, Math.Abs((int)sample)); if (sample != 0) nonzero++; destination?.Add(sample); }
        }
        return (frames, peak, nonzero, Convert.ToHexStringLower(digest.GetHashAndReset()));
    }

    internal static Statistics Summarize(IReadOnlyList<FrameRow> rows, double nativeFps)
    {
        if (rows.Count == 0) throw new ArgumentException("No measured frames.", nameof(rows));
        double[] sorted = rows.Select(row => row.WallMs).Order().ToArray();
        double median = sorted.Length % 2 == 0 ? (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2 : sorted[sorted.Length / 2];
        double mean = sorted.Average();
        return new(median, mean, sorted[(int)Math.Ceiling(sorted.Length * .95) - 1], sorted[0], sorted[^1],
            1000 / mean, 1000 / mean / nativeFps * 100, rows.Sum(row => row.AllocatedBytes), rows.Sum(row => row.GcPauseMs));
    }

    internal static Repeatability SummarizeRepetitions(double[] means, double[] medians)
    {
        if (means.Length == 0 || means.Length != medians.Length
            || means.Concat(medians).Any(value => !double.IsFinite(value) || value <= 0))
            throw new ArgumentException("Finite, positive observations from matching repetitions are required.");
        double mean = means.Average();
        double sd = means.Length < 2 ? 0 : Math.Sqrt(means.Sum(value => (value-mean)*(value-mean))/(means.Length-1));
        static double Median(double[] values) { var sorted = values.Order().ToArray(); return sorted.Length%2==0
            ? (sorted[sorted.Length/2-1]+sorted[sorted.Length/2])/2 : sorted[sorted.Length/2]; }
        double center = Median(means);
        return new(means.Length,(double[])means.Clone(),(double[])medians.Clone(),mean,means.Min(),means.Max(),sd,
            100*sd/mean,Median(means.Select(value=>Math.Abs(value-center)).ToArray()));
    }

    internal static string AssessPerformance(bool sameBinary, string beforePolicy, string afterPolicy,
        Repeatability? before, Repeatability? after, bool binaryIdentityAvailable = true)
    {
        if (beforePolicy != afterPolicy) return "IncomparableTimingPolicy";
        if (!binaryIdentityAvailable) return "IncompleteBinaryIdentity";
        if (sameBinary) return "SameBinaryRepeatabilityControl";
        if (before is null || after is null || before.Repetitions < 5 || after.Repetitions < 5)
            return "InsufficientRepetitions";
        // Sequential series can drift together with host load/temperature.
        // Their dispersion is useful, but does not replace alternating A/B runs.
        return "UnpairedSeriesNoCausalConclusion";
    }

    internal static CpuProfile AnalyzeCpu(JsonElement document)
    {
        string[] names = document.GetProperty("shared").GetProperty("frames").EnumerateArray().Select(frame => frame.GetProperty("name").GetString()!).ToArray();
        var own = new Dictionary<string, double>(); var inclusive = new Dictionary<string, double>();
        double total = 0, blocked = 0;
        void Add(int[] stack, double weight)
        {
            if (weight <= 0 || !stack.Any(id => names[id].Contains("SegaEmu.Services.EmulatorCore.RunFrame(", StringComparison.Ordinal))) return;
            total += weight;
            if (stack.Any(id => names[id] == "BLOCKED_TIME")) { blocked += weight; return; }
            var real = stack.Select(id => names[id]).Where(name => name is not ("CPU_TIME" or "UNMANAGED_CODE_TIME" or "BLOCKED_TIME")).ToArray();
            if (real.Length > 0) own[real[^1]] = own.GetValueOrDefault(real[^1]) + weight;
            foreach (string name in real.Distinct()) inclusive[name] = inclusive.GetValueOrDefault(name) + weight;
        }
        foreach (var profile in document.GetProperty("profiles").EnumerateArray())
        {
            string unit = profile.GetProperty("unit").GetString()!;
            double scale = unit switch { "milliseconds" => 1, "seconds" => 1000, "microseconds" => .001, "nanoseconds" => .000001, _ => throw new InvalidDataException("Unsupported trace time unit: " + unit) };
            if (profile.GetProperty("type").GetString() == "evented")
            {
                var stack = new List<int>(); double previous = profile.GetProperty("startValue").GetDouble();
                foreach (var item in profile.GetProperty("events").EnumerateArray())
                {
                    double at = item.GetProperty("at").GetDouble(); Add(stack.ToArray(), (at - previous) * scale);
                    int frame = item.GetProperty("frame").GetInt32();
                    if (item.GetProperty("type").GetString() == "O") stack.Add(frame);
                    else { if (stack.Count == 0 || stack[^1] != frame) throw new InvalidDataException("Unbalanced trace stack."); stack.RemoveAt(stack.Count - 1); }
                    previous = at;
                }
            }
            else
            {
                var samples = profile.GetProperty("samples").EnumerateArray().ToArray();
                if (!profile.TryGetProperty("weights", out var weights)) throw new InvalidDataException("Sample weights are required.");
                int index = 0; foreach (var weight in weights.EnumerateArray()) Add(samples[index++].EnumerateArray().Select(value => value.GetInt32()).ToArray(), weight.GetDouble() * scale);
            }
        }
        var groups = new Dictionary<string, double>();
        foreach (var pair in own) { string group = Group(pair.Key); groups[group] = groups.GetValueOrDefault(group) + pair.Value; }
        var methods = inclusive.Keys.Union(own.Keys).Select(name => new Cost(name, own.GetValueOrDefault(name), inclusive.GetValueOrDefault(name),
            total == 0 ? 0 : own.GetValueOrDefault(name) / total * 100, total == 0 ? 0 : inclusive.GetValueOrDefault(name) / total * 100)).OrderByDescending(cost => cost.OwnMs).ToArray();
        return new(total, blocked, methods, groups);
    }
    private static string Group(string name)
    {
        int signature = name.IndexOf('(');
        string owner = signature < 0 ? name : name[..signature];
        return owner.Contains("Chips.CPU.", StringComparison.Ordinal) ? "CPU"
            : owner.Contains("Chips.Video.", StringComparison.Ordinal) ? "VDP"
            : owner.Contains("Chips.Memory.", StringComparison.Ordinal) ? "Memory"
            : owner.Contains("Chips.Audio.", StringComparison.Ordinal) ? "PSG"
            : owner.Contains("Chips.Logic.", StringComparison.Ordinal) ? "Logic"
            : owner.Contains("Sg1000Board", StringComparison.Ordinal) ? "Wiring"
            : owner.Contains("EmulatorCore", StringComparison.Ordinal) ? "NativeBackend" : "RuntimeAndOther";
    }

    private static bool SameNativeFrame(FrameRow a, FrameRow b)
        => a.Frame == b.Frame && a.Phase == b.Phase && a.MachineNanoseconds == b.MachineNanoseconds
            && a.Pc == b.Pc && a.Instructions == b.Instructions && a.CpuClocks == b.CpuClocks
            && a.MasterHalfEdges == b.MasterHalfEdges && a.KnownPixels == b.KnownPixels
            && a.ColourMask == b.ColourMask && a.VdpQualified == b.VdpQualified && a.CpuFaulted == b.CpuFaulted
            && a.IntAsserted == b.IntAsserted && a.NmiPending == b.NmiPending
            && a.AudioFrames == b.AudioFrames && a.AudioPeak == b.AudioPeak && a.NonzeroSamples == b.NonzeroSamples
            && a.VideoHash == b.VideoHash && a.PcmHash == b.PcmHash;

    private static Comparison? FindComparison(Request request, Result current, string gameDirectory)
    {
        Pass? measured = current.Passes.FirstOrDefault(pass => pass.Kind == "Measure");
        if (measured is null || current.Error is not null) return null;
        IEnumerable<string> files = request.CompareTo is { Length: > 0 } selected ? new[] { selected }
            : Directory.EnumerateFiles(gameDirectory, "report.json", SearchOption.AllDirectories).OrderByDescending(File.GetLastWriteTimeUtc);
        foreach (string path in files)
        {
            Result? previous = JsonSerializer.Deserialize<Result>(File.ReadAllText(path), Json);
            Pass? before = previous?.Passes.FirstOrDefault(pass => pass.Kind == "Measure");
            if (before is null || previous!.Error is not null || previous.Sha256 != current.Sha256
                || string.IsNullOrEmpty(request.CompareTo) && previous.Execution != "Reference"
                || previous.Layout != current.Layout || previous.FrameCount != current.FrameCount || previous.WarmupFrames != current.WarmupFrames
                || previous.Host != current.Host || previous.Runtime != current.Runtime || previous.ScenarioVersion != current.ScenarioVersion
                || before.JitOptimizationsDisabled != measured.JitOptimizationsDisabled) continue;
            return new(path, before.Statistics.MedianMs, measured.Statistics.MedianMs,
                100 * (1 - measured.Statistics.MedianMs / before.Statistics.MedianMs),
                measured.StateHash == before.StateHash, measured.FinalVideoHash == before.FinalVideoHash, measured.AudioHash == before.AudioHash,
                measured.Frames.Length, measured.Frames.Length == before.Frames.Length
                    && measured.Frames.Zip(before.Frames).All(pair => SameNativeFrame(pair.First, pair.Second)),
                AssessPerformance(before.BinaryFingerprint == measured.BinaryFingerprint, previous.TimingPolicy, current.TimingPolicy,
                    previous.Repeatability, current.Repeatability,
                    before.BinaryFingerprint.Length > 0 && measured.BinaryFingerprint.Length > 0),
                previous.Repeatability?.MeanMs,current.Repeatability?.MeanMs,
                previous.Repeatability is { } oldRuns && current.Repeatability is { } newRuns
                    ? 100*(1-newRuns.MeanMs/oldRuns.MeanMs) : null);
        }
        return null;
    }

    private static string Markdown(Result result)
    {
        var text = new StringBuilder(); text.AppendLine("# " + Path.GetFileName(result.Rom));
        text.AppendLine($"\n{result.Status}; {result.Bytes} bytes; {result.Layout}; {result.Runtime}.\n\nSHA256: `{result.Sha256}`\n");
        if (result.Error is not null) text.AppendLine("```text\n" + result.Error + "\n```\n");
        text.AppendLine($"Timing policy: {result.TimingPolicy}. Native repetitions use fresh cores and identical guest warmup/input sequences.\n");
        if (result.Passes.Any(pass => pass.Kind == "Prime"))
            text.AppendLine("Prime executes the complete same scenario before the native series; it is retained separately and excluded by design, not discarded after inspecting measured values.\n");
        if (result.Repeatability is { } repeat)
        {
            text.AppendLine(FormattableString.Invariant($"Repetitions={repeat.Repetitions}; mean of run means={repeat.MeanMs:F3}ms/frame; range of run means={repeat.MinimumRunMeanMs:F3}..{repeat.MaximumRunMeanMs:F3}; sample SD={repeat.StandardDeviationMs:F3}ms; CV={repeat.CoefficientVariationPercent:F3}%; MAD={repeat.MedianAbsoluteDeviationMs:F3}ms.\n"));
            text.AppendLine("| Repetition | Mean ms/frame | Median ms/frame |\n|---|---:|---:|");
            for (int i=0;i<repeat.Repetitions;i++)
                text.AppendLine(FormattableString.Invariant($"| {i+1} | {repeat.RunMeansMs[i]:F3} | {repeat.RunMediansMs[i]:F3} |"));
            text.AppendLine("\nDispersion is between complete repeated scenarios, not an IID confidence interval over correlated frames. A single series is calibration; causal before/after attribution requires repeated alternating A/B acquisition.\n");
        }
        text.AppendLine("| Pass | Median ms/frame | Mean | P95 | Host FPS | Native % | Allocated bytes in frames | GC pause ms |\n|---|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var pass in result.Passes)
        {
            var s = pass.Statistics;
            text.AppendLine(FormattableString.Invariant($"| {pass.Kind} | {s.MedianMs:F3} | {s.MeanMs:F3} | {s.P95Ms:F3} | {s.HostFps:F3} | {s.NominalPercent:F3} | {s.AllocatedBytes} | {s.GcPauseMs:F3} |"));
            text.AppendLine($"\n{pass.Kind}: JIT optimizations disabled={pass.JitOptimizationsDisabled}; core SHA256 `{pass.CoreSha256}`.\n");
            if (pass.BinaryHashes is { } identities)
                foreach(var binary in identities) text.AppendLine($"- {binary.Key} SHA256: `{binary.Value}`");
            if (pass.Frames.LastOrDefault() is { } counts)
            {
                text.AppendLine($"Cumulative counts, including warmup: CPU fences={counts.BusRangeDeliveries}; CPU words={counts.CpuOutputWords}; logic inputs={counts.LogicInputWords}; memory inputs={counts.MemoryInputWords}; VRAM read forms={counts.VramReadWaveDeliveries}; explicit DRAM pin deliveries={counts.DramExplicitPinDeliveries}; video synchronizations={counts.VideoSynchronizationCalls}; native samples={counts.NativeVideoSamples}.\n");
                text.AppendLine($"VRAM transport counts: DRAM output callbacks={counts.DramOutputNotifications}; VDP data deliveries={counts.VramDataDeliveries}; coincident groups={counts.VramDataGroups}.\n");
                text.AppendLine($"Represented VRAM outputs: DRAM programs={counts.DramReadOutputPrograms}; shared input words={counts.VramReadProgramWords}; declined read groups={counts.DramReadOutputFallbacks}.\n");
                text.AppendLine($"Cumulative software work: bus resolutions={counts.CpuBusResolutions}; held WAIT/control samples={counts.HeldWaitSamples}/{counts.HeldControlSamples}; direct/gate cause visits={counts.DirectCauseVisits}/{counts.GateCauseVisits}; skipped CPU words inspected by the gate filter={counts.GateSkippedCpuWords}; omitted memory response records={counts.MemoryResponseRecordsOmitted}. The filter inspections are work, not eliminated operations.\n");
            }
            foreach (var group in pass.Frames.GroupBy(row => row.Phase))
                text.AppendLine(FormattableString.Invariant($"- {group.Key}: {group.Count()} frames, mean {group.Average(row => row.WallMs):F3}ms, CPU {group.Average(row => row.ProcessCpuMs):F3}ms/frame."));
            if (pass.Cpu is { } cpu)
            {
                text.AppendLine(FormattableString.Invariant($"\nScoped sampled time: {cpu.ScopedSampleMs:F3}ms; blocked samples: {cpu.BlockedSampleMs:F3}ms.\n"));
                text.AppendLine("| Method | Own % | Inclusive % |\n|---|---:|---:|");
                foreach (var cost in cpu.Methods.Take(40)) text.AppendLine(FormattableString.Invariant($"| `{cost.Name.Replace("|", "\\|")}` | {cost.OwnPercent:F3} | {cost.InclusivePercent:F3} |"));
                text.AppendLine("\nInclusive costs contain descendants and must not be added. Weights are samples, not invocation counts.\n");
            }
        }
        if (result.Comparison is { } comparison)
        {
            text.AppendLine(FormattableString.Invariant($"\nDescriptive comparison (first native passes): {comparison.PreviousMedianMs:F3}→{comparison.CurrentMedianMs:F3}ms/frame, relative time difference={comparison.TimeReductionPercent:F3}%; assessment={comparison.PerformanceAssessment}; final state/video/audio equal={comparison.FinalStateEqual}/{comparison.VideoEqual}/{comparison.AudioEqual}. This is not a measured causal gain.\n"));
            if (comparison.PreviousRepeatedMeanMs is {} oldMean && comparison.CurrentRepeatedMeanMs is {} newMean)
                text.AppendLine(FormattableString.Invariant($"Repeated-series means: {oldMean:F3}→{newMean:F3}ms/frame; descriptive relative difference={comparison.RepeatedMeanTimeDifferencePercent:F3}%. Assessment remains {comparison.PerformanceAssessment}.\n"));
        }
        text.AppendLine("\nMeasure runs have no trace. Profile runs include tracing overhead. Core emulation is captured; application UI/audio-device/GPU costs are outside this scope. GC counts/pauses are process-wide; allocated bytes are the synchronous frame thread only. Input phases describe the host commands, not proof that a game reached gameplay.\n");
        return text.ToString();
    }

    private sealed class SelfTrace : IAsyncDisposable
    {
        private object? _session;
        private readonly FileStream _file;
        private readonly Task _copy;
        private SelfTrace(object session, FileStream file, Stream events)
        { _session = session; _file = file; _copy = events.CopyToAsync(file); }
        internal static SelfTrace Start(string assemblyPath, string output)
        {
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            Type providerType = assembly.GetType("Microsoft.Diagnostics.NETCore.Client.EventPipeProvider", throwOnError: true)!;
            Type clientType = assembly.GetType("Microsoft.Diagnostics.NETCore.Client.DiagnosticsClient", throwOnError: true)!;
            Array providers = Array.CreateInstance(providerType, 2);
            var constructor = providerType.GetConstructors().Single(item => item.GetParameters().Length == 4);
            providers.SetValue(constructor.Invoke(["Microsoft-DotNETCore-SampleProfiler", EventLevel.Informational, 0xF00000000000L, null]), 0);
            providers.SetValue(constructor.Invoke(["Microsoft-Windows-DotNETRuntime", EventLevel.Verbose, 0x100003801DL, null]), 1);
            object client = Activator.CreateInstance(clientType, Environment.ProcessId)!;
            MethodInfo start = clientType.GetMethods().Single(method => method.Name == "StartEventPipeSession"
                && method.GetParameters().Length == 3 && method.GetParameters()[0].ParameterType.IsGenericType);
            object session = start.Invoke(client, [providers, true, 64])!;
            FileStream? file = null;
            try
            {
                file = File.Create(output);
                Stream events = (Stream)session.GetType().GetProperty("EventStream")!.GetValue(session)!;
                return new(session, file, events);
            }
            catch { try { ((IDisposable)session).Dispose(); } finally { file?.Dispose(); } throw; }
        }
        public async ValueTask DisposeAsync()
        {
            object? session = Interlocked.Exchange(ref _session, null);
            if (session is null) return;
            try { session.GetType().GetMethod("Stop", Type.EmptyTypes)!.Invoke(session, null); await _copy.ConfigureAwait(false); }
            finally { try { ((IDisposable)session).Dispose(); } finally { await _file.DisposeAsync().ConfigureAwait(false); } }
        }
    }

    private static async Task ConvertTrace(string tool, string directory)
    {
        using var process = new Process { StartInfo = new(tool) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true } };
        foreach (string argument in new[] { "convert", Path.Combine(directory, "cpu.nettrace"), "--format", "Speedscope", "--output", Path.Combine(directory, "cpu") }) process.StartInfo.ArgumentList.Add(argument);
        bool started = false;
        try
        {
            started = process.Start(); if (!started) throw new InvalidOperationException("Trace conversion did not start.");
            Task<string> stdout = process.StandardOutput.ReadToEndAsync(), stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(90)).ConfigureAwait(false);
            string log = await stdout.ConfigureAwait(false) + "\n" + await stderr.ConfigureAwait(false);
            await File.WriteAllTextAsync(Path.Combine(directory, "conversion.log"), log).ConfigureAwait(false);
            if (process.ExitCode != 0) throw new InvalidOperationException("Trace conversion failed: " + log);
        }
        finally { if (started && !process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync().ConfigureAwait(false); } }
    }
    private static void WriteWav(string path, List<short> samples, int sampleRate)
    {
        using var file = File.Create(path); using var writer = new BinaryWriter(file, Encoding.ASCII, leaveOpen: false);
        writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples.Count * 2); writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
        writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(sampleRate); writer.Write(sampleRate * 2);
        writer.Write((short)2); writer.Write((short)16); writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(samples.Count * 2);
        foreach (short sample in samples) writer.Write(sample);
    }
    private static string Hash(ReadOnlySpan<byte> bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
