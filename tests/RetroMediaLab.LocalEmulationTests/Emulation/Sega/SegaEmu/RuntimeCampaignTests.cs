using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Sega.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Sega.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Functions;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Factories;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services;

namespace RetroMediaLab.Tests.Emulation.Sega.SegaEmu;

// Temporary ROM-dependent campaign. Results and captures survive removal of this fixture.
public sealed class RuntimeCampaignTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private sealed record Scenario(string Model, string Folder, string Media, int Warmup, int Frames, string? BaselineDirectory, string[] Captures);
    private sealed class Result
    {
        public string Model { get; set; } = "";
        public string Media { get; set; } = "";
        public string? Error { get; set; }
        public bool Loaded { get; set; }
        public int Frames { get; set; }
        public int WarmupFrames { get; set; }
        public double InitializationSeconds { get; set; }
        public double EngineSeconds { get; set; }
        public double ObservationSeconds { get; set; }
        public double ProcessCpuSeconds { get; set; }
        public double Fps { get; set; }
        public double NominalFps { get; set; }
        public double RealTimePercent { get; set; }
        public double SimulatedCpuCycles { get; set; }
        public double EffectiveCpuHz { get; set; }
        public long CoreAllocatedBytes { get; set; }
        public double CoreAllocatedBytesPerFrame { get; set; }
        public long ManagedBytesBefore { get; set; }
        public long ManagedBytesAfter { get; set; }
        public long WorkingSetPeak { get; set; }
        public int AudibleFrames { get; set; }
        public long AudioFrames { get; set; }
        public int DistinctSampledFrames { get; set; }
        public bool StateMatches { get; set; }
        public bool StateVideoMatches { get; set; }
        public int StateDifferenceCount { get; set; }
        public List<object> StateDifferences { get; set; } = new();
        public bool AdapterPassed { get; set; }
        public bool MediaUnmodified { get; set; }
        public bool FirmwareUnmodified { get; set; }
        public int BaselineComparedCaptures { get; set; }
        public int BaselineDifferentCaptures { get; set; }
        public Dictionary<string,string> MediaSha256 { get; set; } = new();
        public Dictionary<string,string> FirmwareSha256 { get; set; } = new();
        public List<object> Screenshots { get; set; } = new();
    }

    [Fact]
    public void RunAllAvailableMachineScenarios()
    {
        string root = FindRoot();
        Assembly assembly = typeof(SegaBoard).Assembly;
        string configuration = assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
        bool debug = configuration == "Debug";
        Assert.Equal(debug, assembly.GetCustomAttribute<DebuggableAttribute>()!.IsJITOptimizerDisabled);
        string? selectedModel=Environment.GetEnvironmentVariable("SEGAEMU_TEST_MODEL");
        string output = Path.Combine(root,"artifacts/SegaEmu/results",configuration+(string.IsNullOrEmpty(selectedModel) ? "" : "-diagnostic"));
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output,"runtime.json"),JsonSerializer.Serialize(new {
            Configuration=configuration, assembly.Location, Sha256=HashFile(assembly.Location),
            assembly.ManifestModule.ModuleVersionId, Framework=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            Environment.ProcessorCount, Debugger.IsAttached,
            Protocol="RunFrame-only engine time/allocations; screenshots, input, state replay and adapter smoke excluded. Historical GXCS scenarios; zero-based input indices."
        },JsonOptions));
        List<Result> results = new();
        List<Scenario> scenarios = Scenarios(root);
        if (!string.IsNullOrEmpty(selectedModel)) scenarios=scenarios.Where(scenario=>scenario.Model==selectedModel).ToList();
        foreach (Scenario scenario in scenarios)
        {
            Result result = RunScenario(scenario,output);
            results.Add(result);
            File.WriteAllText(Path.Combine(output,"results.json"),JsonSerializer.Serialize(results,JsonOptions));
            Console.WriteLine($"{configuration} {scenario.Model} {Path.GetFileName(scenario.Media)}: {result.Fps:F2} FPS; state={result.StateMatches}/{result.StateVideoMatches}; error={result.Error}");
        }
        Assert.All(results,result => Assert.True(result.Error is null && result.Loaded && result.MediaUnmodified && result.FirmwareUnmodified && result.StateMatches && result.StateVideoMatches && result.AdapterPassed && result.BaselineDifferentCaptures==0,
            $"{result.Model}: {result.Media}: {result.Error}; state={result.StateMatches}/{result.StateVideoMatches}; adapter={result.AdapterPassed}"));
    }

    private static List<Scenario> Scenarios(string root)
    {
        List<Scenario> result = new();
        foreach ((string model,string folder,int warmup,int frames) in new[] {
            ("Sg1000","sg1000",0,180), ("MasterSystem","master-system",60,600),
            ("GameGear","game-gear",60,1200), ("MegaDrive","mega-drive",60,1200), ("MegaCd","mega-cd",60,1800) })
        {
            string baseline = Path.Combine(root,"artifacts/GenesisPlusGXCS/release",folder);
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(baseline,"results.json")));
            foreach (JsonElement entry in document.RootElement.EnumerateArray())
            {
                string media = entry.GetProperty("Media").GetString()!;
                if (!File.Exists(media))
                {
                    string directory = model switch {"GameGear"=>"Game Gear","MegaDrive"=>"Mega Drive - Genesis",_=>throw new FileNotFoundException(media)};
                    media = Path.Combine(@"G:\Retro\Cartouches\Sega",directory,Path.GetFileName(media));
                }
                string[] captures = entry.TryGetProperty("Screenshots",out JsonElement shots) ? shots.EnumerateArray().Select(value=>value.GetString()!).ToArray() : [];
                int requested = entry.TryGetProperty("RequestedFrames",out JsonElement count) ? count.GetInt32() : frames;
                result.Add(new(model,folder,media,warmup,requested,baseline,captures));
            }
        }
        Scenario sg = result.First(value=>value.Model=="Sg1000");
        Scenario sms = result.First(value=>value.Model=="MasterSystem");
        result.Add(sg with {Model="Sc3000",Folder="sc3000",BaselineDirectory=null,Captures=[]});
        result.Add(sms with {Model="MarkIII",Folder="mark-iii",BaselineDirectory=null,Captures=[]});
        return result;
    }

    private static Result RunScenario(Scenario scenario, string output)
    {
        Result result = new() {Model=scenario.Model,Media=scenario.Media,WarmupFrames=scenario.Warmup};
        SegaBoard? board = null; Process? process = null;
        string directory = Path.Combine(output,scenario.Folder); Directory.CreateDirectory(directory);
        Dictionary<string,string> hashes = MediaFiles(scenario.Media).Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(path=>path,HashFile,StringComparer.OrdinalIgnoreCase);
        result.MediaSha256 = hashes;
        result.FirmwareSha256 = FirmwarePaths().ToDictionary(entry=>entry.Path,entry=>HashFile(entry.Path),StringComparer.OrdinalIgnoreCase);
        try
        {
            byte[] bitmap = new byte[1024 * 626 * 4];
            short[] audio = new short[9600];
            SystemConfiguration settings = new();
            settings.Video.render = 1;
            long init = Stopwatch.GetTimestamp();
            board = BoardFactory.Create(scenario.Model,new(settings,bitmap,1024,626,4096,44100,OpenFile,Firmware,CancellationToken.None));
            result.Loaded = board.Load(scenario.Media);
            if (!result.Loaded) throw new InvalidDataException("The board refused the media.");
            board.RefreshConfiguration();
            result.InitializationSeconds = Stopwatch.GetElapsedTime(init).TotalSeconds;
            for (int frame = 0; frame < scenario.Warmup; frame++) board.RunFrame(audio);
            result.NominalFps = board.NominalFrameRate;
            result.ManagedBytesBefore = GC.GetTotalMemory(false);
            process = Process.GetCurrentProcess();
            TimeSpan cpuStart = process.TotalProcessorTime;
            long observation = Stopwatch.GetTimestamp(), ticks = 0, allocations = 0;
            double masterCycles = 0;
            HashSet<string> sampled = new();
            Dictionary<int,string> captures = scenario.Captures.ToDictionary(name=>CaptureFrame(name),name=>name);
            if (captures.Count == 0) captures[scenario.Frames] = Path.GetFileNameWithoutExtension(scenario.Media)+$"-frame-{scenario.Frames}.png";
            for (int frame = 0; frame < scenario.Frames; frame++)
            {
                board.Input.DigitalInputs[0] = Input(scenario.Model,frame);
                long allocated = GC.GetAllocatedBytesForCurrentThread(), start = Stopwatch.GetTimestamp();
                int count = board.RunFrame(audio);
                ticks += Stopwatch.GetTimestamp() - start;
                allocations += GC.GetAllocatedBytesForCurrentThread() - allocated;
                result.Frames++; masterCycles += board.FrameCycles; result.AudioFrames += count;
                if (audio.AsSpan(0,count * 2).ContainsAnyExcept((short)0)) result.AudibleFrames++;
                if ((frame + 1) % 30 == 0)
                {
                    sampled.Add(VideoHash(board,bitmap)); process.Refresh(); result.WorkingSetPeak = Math.Max(result.WorkingSetPeak,process.WorkingSet64);
                }
                if (captures.TryGetValue(frame + 1,out string? name))
                {
                    (byte[] pixels,int width,int height) = Pixels(board,bitmap);
                    string path = Path.Combine(directory,name); WritePng(path,pixels,width,height);
                    string? baseline = scenario.BaselineDirectory is null ? null : Path.Combine(scenario.BaselineDirectory,name);
                    bool? equal = baseline is not null && File.Exists(baseline) ? ImageHash(baseline)==PixelHash(pixels) : null;
                    if (equal.HasValue) { result.BaselineComparedCaptures++; if (!equal.Value) result.BaselineDifferentCaptures++; }
                    result.Screenshots.Add(new {Path=path,Width=width,Height=height,Baseline=baseline,PixelsEqual=equal});
                }
            }
            result.ObservationSeconds = Stopwatch.GetElapsedTime(observation).TotalSeconds;
            result.EngineSeconds = (double)ticks / Stopwatch.Frequency;
            result.CoreAllocatedBytes = allocations; result.CoreAllocatedBytesPerFrame = (double)allocations / result.Frames;
            result.ProcessCpuSeconds = (process.TotalProcessorTime - cpuStart).TotalSeconds;
            result.ManagedBytesAfter = GC.GetTotalMemory(false); result.DistinctSampledFrames = sampled.Count;
            result.Fps = result.Frames / result.EngineSeconds; result.RealTimePercent = result.Fps / result.NominalFps * 100;
            result.SimulatedCpuCycles = masterCycles / (SystemHardware.IsSixteenBit(board.Hardware) ? 7 : 15);
            result.EffectiveCpuHz = result.SimulatedCpuCycles / result.EngineSeconds;
            byte[] saved = new byte[board.StateSize]; Assert.Equal(saved.Length,board.SaveState(saved));
            for (int frame = 0; frame < 8; frame++) board.RunFrame(audio);
            byte[] expected = new byte[board.StateSize]; board.SaveState(expected); string expectedVideo = VideoHash(board,bitmap);
            Assert.Equal(saved.Length,board.LoadState(saved));
            for (int frame = 0; frame < 8; frame++) board.RunFrame(audio);
            byte[] replay = new byte[board.StateSize]; board.SaveState(replay);
            result.StateMatches = expected.AsSpan().SequenceEqual(replay); result.StateVideoMatches = expectedVideo==VideoHash(board,bitmap);
            if (!result.StateMatches)
            {
                for(int offset=0;offset<Math.Min(expected.Length,replay.Length);offset++)
                    if (expected[offset]!=replay[offset])
                    {
                        result.StateDifferenceCount++;
                        if (result.StateDifferences.Count<64) result.StateDifferences.Add(new {Offset=offset,Expected=expected[offset],Actual=replay[offset]});
                    }
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SEGAEMU_TEST_MODEL")))
                {
                    string stem=Path.Combine(directory,Path.GetFileNameWithoutExtension(scenario.Media));
                    File.WriteAllBytes(stem+"-saved.bin",saved);
                    File.WriteAllBytes(stem+"-expected.bin",expected);
                    File.WriteAllBytes(stem+"-replay.bin",replay);
                }
            }
            result.AdapterPassed = AdapterSmoke(scenario,output);
        }
        catch (Exception error) { result.Error = error.ToString(); }
        finally
        {
            try { board?.Dispose(); }
            finally { process?.Dispose(); }
            result.MediaUnmodified = hashes.All(entry=>HashFile(entry.Key)==entry.Value);
            result.FirmwareUnmodified = result.FirmwareSha256.All(entry=>HashFile(entry.Key)==entry.Value);
        }
        return result;
    }

    private static bool AdapterSmoke(Scenario scenario,string output)
    {
        string session = Path.Combine(output,"sessions",Guid.NewGuid().ToString("N"));
        ManagedCore? core = null;
        try
        {
            Dictionary<string,string> firmware = new();
            foreach ((string key,string path) in FirmwarePaths()) firmware[key]=path;
            MachineConfiguration config = CoreSettingsFunctions.Configure(new(scenario.Model,"segaemu",Media:[new(scenario.Media,
                scenario.Model=="MegaCd" ? MediaCategory.CompactDisc : MediaCategory.Cartridge,IsReadOnly:true)],FirmwarePaths:firmware));
            core = new(); core.Initialize(config,session); core.SetInput(EmulationInputSnapshot.Empty);
            for (int frame = 0; frame < 120; frame++)
            {
                core.RunFrame();
                while (core.TryDequeueAudio(out _)) { }
            }
            Assert.NotNull(core.LatestVideoFrame); Assert.NotNull(core.LatestAudioChunk);
            core.LoadState(core.SaveState()); core.RunFrame();
            if (scenario.Model=="MegaCd")
            {
                core.EjectMedia(new(RetroMediaLab.Emulation.Enums.EmulationMediaCategory.CompactDiscDrive,0));
                core.InsertMedia(new(scenario.Media,MediaCategory.CompactDisc,IsReadOnly:true)); core.RunFrame();
            }
            return true;
        }
        finally
        {
            try { core?.Dispose(); }
            finally
            {
                string resolved = Path.GetFullPath(session), parent = Path.GetFullPath(Path.Combine(output,"sessions")) + Path.DirectorySeparatorChar;
                if (!resolved.StartsWith(parent,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unexpected session path.");
                if (Directory.Exists(resolved)) Directory.Delete(resolved,true);
            }
        }
    }

    private static ushort Input(string model,int frame)
    {
        if (model is "Sg1000" or "Sc3000") return (ushort)((frame is >=60 and <63 ? 128 : 0) | (frame is >=120 and <140 ? 16 : 0));
        if (model is "MasterSystem" or "MarkIII") return (ushort)((frame is >=180 and <184 ? 16 : 0) | (frame is >=240 and <244 ? 32 : 0) | (frame is >=300 and <420 ? 8 : 0));
        int start = model=="MegaCd" ? 1080 : 540, end = model=="MegaCd" ? 1380 : 840;
        return (ushort)((frame is >=180 and <184 or >=480 and <484 || model=="MegaCd" && frame is >=900 and <904 ? 128 : 0) |
            (frame>=start && frame<end ? 8 | ((frame-start)%60<4 ? 16 : 0) : 0));
    }
    private static int CaptureFrame(string name) => int.Parse(Regex.Match(name,@"frame-(\d+)").Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture);
    private static Stream? OpenFile(string path) => File.Exists(path) ? new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,65536,FileOptions.RandomAccess) : null;
    private static IEnumerable<(string Key,string Path)> FirmwarePaths()
    {
        string folder = @"G:\Retro\Cartouches\Sega\Mega Drive - Genesis";
        yield return ("configuration.firmware.segaemu.1",Path.Combine(folder,"[BIOS] Mega-CD (Europe) (v1.00).md"));
        yield return ("configuration.firmware.segaemu.2",Path.Combine(folder,"[BIOS] Sega CD (USA) (v1.10).md"));
        yield return ("configuration.firmware.segaemu.3",Path.Combine(folder,"[BIOS] Mega-CD (Japan) (1.00S).md"));
    }
    private static int Firmware(int id,Memory<byte> destination)
    {
        if ((byte)id!=0x84) return 0;
        string key = (byte)(id>>8) switch {0xc0=>"configuration.firmware.segaemu.1",0x80=>"configuration.firmware.segaemu.2",_=>"configuration.firmware.segaemu.3"};
        string path = FirmwarePaths().Single(entry=>entry.Key==key).Path;
        Stream? stream = null;
        try { stream = File.OpenRead(path); return stream.ReadAtLeast(destination.Span,Math.Min((int)stream.Length,destination.Length)); }
        finally { stream?.Dispose(); }
    }
    private static IEnumerable<string> MediaFiles(string path)
    {
        yield return path;
        if (!Path.GetExtension(path).Equals(".cue",StringComparison.OrdinalIgnoreCase)) yield break;
        foreach (string line in File.ReadLines(path))
        {
            Match match = Regex.Match(line,"^\\s*FILE\\s+\"([^\"]+)\"",RegexOptions.IgnoreCase);
            if (match.Success) yield return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!,match.Groups[1].Value));
        }
    }
    private static (byte[] Pixels,int Width,int Height) Pixels(SegaBoard board,byte[] bitmap)
    {
        int width=board.Video.OutputWidth, height=board.Video.ViewportHeight+board.Video.ViewportY*2;
        if (board.Video.interlaced!=0) height*=2;
        byte[] pixels=new byte[width*height*4];
        for (int y=0;y<height;y++) bitmap.AsSpan(y*4096,width*4).CopyTo(pixels.AsSpan(y*width*4));
        return (pixels,width,height);
    }
    private static string VideoHash(SegaBoard board,byte[] bitmap) => PixelHash(Pixels(board,bitmap).Pixels);
    private static string PixelHash(byte[] pixels)
    {
        for (int index=3;index<pixels.Length;index+=4) pixels[index]=255;
        return Convert.ToHexString(SHA256.HashData(pixels));
    }
    private static string ImageHash(string path)
    {
        Stream? stream=null;
        try
        {
            stream=File.OpenRead(path); BitmapDecoder decoder=BitmapDecoder.Create(stream,BitmapCreateOptions.None,BitmapCacheOption.OnLoad);
            FormatConvertedBitmap image=new(decoder.Frames[0],PixelFormats.Bgr32,null,0); image.Freeze();
            byte[] pixels=new byte[image.PixelWidth*image.PixelHeight*4]; image.CopyPixels(pixels,image.PixelWidth*4,0);
            return PixelHash(pixels);
        }
        finally { stream?.Dispose(); }
    }
    private static void WritePng(string path,byte[] pixels,int width,int height)
    {
        Stream? stream=null;
        try
        {
            BitmapSource bitmap=BitmapSource.Create(width,height,96,96,PixelFormats.Bgr32,null,pixels,width*4); bitmap.Freeze();
            PngBitmapEncoder encoder=new(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            stream=File.Create(path); encoder.Save(stream);
        }
        finally { stream?.Dispose(); }
    }
    private static string HashFile(string path)
    {
        Stream? stream=null; try { stream=File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(stream)); }
        finally { stream?.Dispose(); }
    }
    private static string FindRoot()
    {
        DirectoryInfo? directory=new(AppContext.BaseDirectory);
        while(directory is not null && !Directory.Exists(Path.Combine(directory.FullName,"src/RetroMediaLab.Emulation.Sega"))) directory=directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Workspace root not found.");
    }
}
