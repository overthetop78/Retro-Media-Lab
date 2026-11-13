using System.Globalization;
using System.IO;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cpu;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS;

internal sealed partial class SystemCore
{
    byte ISmsCartridgeBus.Hardware { get => system_hw; set => system_hw = value; }
    byte ISmsCartridgeBus.BootRomFlags { get => system_bios; set => system_bios = value; }
    byte ISmsCartridgeBus.Region { get => region_code; set => region_code = value; }
    byte ISmsCartridgeBus.LastZ80Fetch => _z80.LastFetch;
    ReadOnlyMemory<byte> ISmsCartridgeBus.IoRegisters => _input.IoRegisters;
    int ISmsCartridgeBus.LoadBios(int hardware) => LoadBios(hardware);
    int IMdCartridgeBus.DiscLoaded => _cd.Drive.Loaded;
    int IMdCartridgeBus.SampleRate => _sampleRate;
    bool IMdCartridgeBus.CdAudioAllocated => _cdAudioAttached;
    Memory<byte> IMdCartridgeBus.BootRom => boot_rom;
    uint IMdCartridgeBus.ReadOpenBus8(uint address) => m68k_read_bus_8(address);
    uint IMdCartridgeBus.ReadOpenBus16(uint address) => m68k_read_bus_16(address);
    void IMdCartridgeBus.WriteUnused8(uint address, uint data) => m68k_unused_8_w(address, data);
    void IMdCartridgeBus.WriteUnused16(uint address, uint data) => m68k_unused_16_w(address, data);
    uint IMdCartridgeBus.ReadUnusedZ80Bank(uint address) => zbank_unused_r(address);
    void IMdCartridgeBus.WriteUnusedZ80Bank(uint address, uint data) => zbank_unused_w(address, data);
    void IMdCartridgeBus.ResetMainCpu() => _mainCpu.Reset();
    void IMdCartridgeBus.WriteControlIoByte(uint address, uint data) => ctrl_io_write_byte(address, data);
    int IMdCartridgeBus.LoadLockOnRom(int kind, Span<byte> destination)
    {
        // Firmware identifiers 2/3/4 are the original lock-on selectors.
        byte[] bytes = new byte[destination.Length];
        int count = _loadFirmware(kind, bytes);
        if (count > 0) bytes.AsSpan(0, count).CopyTo(destination);
        return count;
    }
    void IMdCartridgeBus.EnsureCdAudio() => EnsureCdAudio();
    void IMdCartridgeBus.EnsureCartridgeAudio() => EnsureCartridgeAudio();
    void IMdCartridgeBus.InitializeYx5200(int sampleRate) => _yx5200.Initialize(sampleRate);
    void IMdCartridgeBus.ResetYx5200() => _yx5200.Reset();
    void IMdCartridgeBus.WriteYx5200(uint data) => _yx5200.Write(data);
    int IMdCartridgeBus.Yx5200ContextSize => Yx5200.ContextSize;
    int IMdCartridgeBus.SaveYx5200Context(Span<byte> state) => _yx5200.SaveContext(state);
    int IMdCartridgeBus.LoadYx5200Context(Span<byte> state) => _yx5200.LoadContext(state);
    void IMdCartridgeBus.ResetMegaSd() => _megaSd.Reset();
    void IMdCartridgeBus.WriteMegaSdRomMapper(uint address, uint data) => _megaSd.WriteRomMapper(address, data);
    void IMdCartridgeBus.WriteMegaSdEnhancedMapper(uint address, uint data) => _megaSd.WriteEnhancedMapper(address, data);
    int IMdCartridgeBus.MegaSdContextSize => _megaSd.ContextSize;
    int IMdCartridgeBus.SaveMegaSdContext(Span<byte> state) => _megaSd.SaveContext(state);
    int IMdCartridgeBus.LoadMegaSdContext(Span<byte> state) => _megaSd.LoadContext(state);

    ref M68kState IScdHost.MainState => ref main;
    double IScdHost.MasterClock => system_clock;
    void IScdHost.RunMain(uint cycles) => m68k_run(cycles);
    uint IScdHost.ReadMainBus8(uint address) => m68k_read_bus_8(address);
    uint IScdHost.ReadMainBus16(uint address) => m68k_read_bus_16(address);
    void IScdHost.WriteMainUnused8(uint address, uint data) => m68k_unused_8_w(address, data);
    void IScdHost.WriteMainUnused16(uint address, uint data) => m68k_unused_16_w(address, data);
    uint IScdHost.ReadZ80Unused(uint address) => zbank_unused_r(address);
    void IScdHost.WriteZ80Unused(uint address, uint data) => zbank_unused_w(address, data);
    void IScdHost.InitializeCdCartridge()
    {
        _cdCartridge.Boot = cd_boot;
        _cdCartridge.Initialize();
        _cd.Boot = _cdCartridge.Boot;
    }
    int IScdHost.MdCartridgeContextSize => _md.ContextSize;
    int IScdHost.SaveMdCartridgeContext(Span<byte> state) => _md.SaveContext(state);
    int IScdHost.LoadMdCartridgeContext(ReadOnlySpan<byte> state) => _md.LoadContext(state.ToArray());
    bool IScdHost.MegaSdEnabled => (cart.special & 0x10) != 0;
    void IScdHost.UpdateMegaSdCdda(uint samples) => _megaSd.UpdateCdda(samples);

    uint IMegaSdBus.MainCycles => unchecked((uint)main.cycles);
    uint IMegaSdBus.CdCyclesPerLine => cd_cycles_per_line;
    byte IMegaSdBus.CdAudioControl { get => scd_regs[0x36 >> 1].High; set => scd_regs[0x36 >> 1].High = value; }
    int IMegaSdBus.DiscLoaded => _cd.Drive.Loaded;
    int IMegaSdBus.CdIndex { get => _cd.Drive.Index; set => _cd.Drive.Index = value; }
    int IMegaSdBus.CdLba { get => _cd.Drive.Lba; set => _cd.Drive.Lba = value; }
    byte IMegaSdBus.CdStatus { get => _cd.Drive.Status; set => _cd.Drive.Status = value; }
    Memory<ushort> IMegaSdBus.CdFader => _cd.Drive.Fader;
    int IMegaSdBus.TrackCount => _cd.Drive.TrackCount;
    int IMegaSdBus.TrackStart(int track) => _cd.Drive.TrackStart(track);
    int IMegaSdBus.TrackEnd(int track) => _cd.Drive.TrackEnd(track);
    int IMegaSdBus.TrackType(int track) => _cd.Drive.TrackType(track);
    int IMegaSdBus.TrackLoopEnabled(int track) => _cd.Drive.TrackLoopEnabled(track);
    int IMegaSdBus.TrackLoopOffset(int track) => _cd.Drive.TrackLoopOffset(track);
    void IMegaSdBus.ResetPcm() => _cd.PcmChip.Reset();
    void IMegaSdBus.ResetCdDrive() => _cd.Drive.Reset();
    void IMegaSdBus.ReadCdAudio(uint samples) => _cd.Drive.ReadAudio(samples);
    void IMegaSdBus.SeekCdAudio(int track, int sector) => _cd.Drive.SeekAudio(track, sector);
    void IMegaSdBus.ReadCdData(Span<byte> destination, Span<byte> subheader) => _cd.Drive.ReadData(destination, subheader);
    byte IMegaSdBus.ReadPcm(uint address, uint cycles) => _cd.PcmChip.Read(address, cycles);
    void IMegaSdBus.WritePcm(uint address, byte data, uint cycles) => _cd.PcmChip.Write(address, data, cycles);
    void IMegaSdBus.WriteUnused8(uint address, uint data) => m68k_unused_8_w(address, data);
    void IMegaSdBus.WriteUnused16(uint address, uint data) => m68k_unused_16_w(address, data);
    void IMegaSdBus.WriteUnusedZ80Bank(uint address, uint data) => zbank_unused_w(address, data);
    int IMegaSdBus.CdContextSize => Cdd.ContextSize;
    int IMegaSdBus.PcmContextSize => Pcm.ContextSize;
    int IMegaSdBus.SaveCdContext(Span<byte> state) => _cd.Drive.SaveContext(state);
    int IMegaSdBus.LoadCdContext(Span<byte> state, string version) => _cd.Drive.LoadContext(state, version);
    int IMegaSdBus.SavePcmContext(Span<byte> state) => _cd.PcmChip.SaveContext(state);
    int IMegaSdBus.LoadPcmContext(Span<byte> state) => _cd.PcmChip.LoadContext(state);

    bool ICartridgeAudioMedia.OpenTrack(ushort index, int minimumDigits, CancellationToken cancellation, out IMp3Playback? playback)
    {
        playback = null;
        cancellation.ThrowIfCancellationRequested();
        string filename = Path.Combine(Path.GetDirectoryName(_mediaPath) ?? "", index.ToString("D" + minimumDigits, CultureInfo.InvariantCulture) + ".mp3");
        Stream? stream;
        try { stream = _openFile(filename); }
        catch (FileNotFoundException) { return false; }
        catch (DirectoryNotFoundException) { return false; }
        if (stream is null) return false;
        bool transferred = false;
        try
        {
            playback = Mp3Playback.Open(stream, leaveOpen: false, cancellation);
            transferred = true;
            return true;
        }
        catch (InvalidDataException) { return true; }
        finally { if (!transferred) stream.Dispose(); }
    }
}
