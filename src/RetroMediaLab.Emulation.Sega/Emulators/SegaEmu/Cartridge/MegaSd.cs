using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

internal interface IMegaSdBus
{
    byte Hardware { get; }
    uint MainCycles { get; }
    uint CdCyclesPerLine { get; }
    byte CdAudioControl { get; set; }
    int DiscLoaded { get; }
    int CdIndex { get; set; }
    int CdLba { get; set; }
    byte CdStatus { get; set; }
    Memory<ushort> CdFader { get; }
    int TrackCount { get; }
    int TrackStart(int track);
    int TrackEnd(int track);
    int TrackType(int track);
    int TrackLoopEnabled(int track);
    int TrackLoopOffset(int track);
    void ResetPcm();
    void ResetCdDrive();
    void ReadCdAudio(uint samples);
    void SeekCdAudio(int track, int sector);
    void ReadCdData(Span<byte> destination, Span<byte> subheader);
    byte ReadPcm(uint address, uint cycles);
    void WritePcm(uint address, byte data, uint cycles);
    void WriteUnused8(uint address, uint data);
    void WriteUnused16(uint address, uint data);
    void WriteUnusedZ80Bank(uint address, uint data);
    int CdContextSize { get; }
    int PcmContextSize { get; }
    int SaveCdContext(Span<byte> state);
    int LoadCdContext(Span<byte> state, string version);
    int SavePcmContext(Span<byte> state);
    int LoadPcmContext(Span<byte> state);
}

internal sealed partial class MegaSd
{
    private readonly IMegaSdBus _bus;
    private readonly CartridgeState cart;
    private readonly BackupRam sram;
    private readonly CartridgeMemoryBank[] memory_map;
    private readonly CartridgeZ80Bank[] zbank_memory_map;
    private readonly Action<uint, uint> m68k_unused_8_w, m68k_unused_16_w, zbank_unused_w;
    private byte system_hw => _bus.Hardware;
    private uint main_cycles => _bus.MainCycles;
    private uint cd_cycles_per_line => _bus.CdCyclesPerLine;
    private byte cd_control { get => _bus.CdAudioControl; set => _bus.CdAudioControl = value; }
    private byte cd_status { get => _bus.CdStatus; set => _bus.CdStatus = value; }
    private int cd_loaded => _bus.DiscLoaded;
    private int cd_index { get => _bus.CdIndex; set => _bus.CdIndex = value; }
    private int cd_lba { get => _bus.CdLba; set => _bus.CdLba = value; }
    private Span<ushort> cd_fader => _bus.CdFader.Span;
    private int track_count => _bus.TrackCount;
    public int ContextSize => NativeContextSize + (system_hw != 0x84 ? _bus.CdContextSize + _bus.PcmContextSize + 1 : 0);

    public MegaSd(IMegaSdBus bus, CartridgeState cartridge, BackupRam backup, CartridgeAddressSpace addresses)
    {
        _bus = bus; cart = cartridge; sram = backup; memory_map = addresses.Main; zbank_memory_map = addresses.Z80;
        m68k_unused_8_w = bus.WriteUnused8; m68k_unused_16_w = bus.WriteUnused16; zbank_unused_w = bus.WriteUnusedZ80Bank;
    }
    public void Reset() => megasd_reset();
    public void WriteEnhancedMapper(uint address, uint data) => megasd_enhanced_ssf2_mapper_w(address, data);
    public void WriteRomMapper(uint address, uint data) => megasd_rom_mapper_w(address, data);
    public void UpdateCdda(uint samples) => megasd_update_cdda(samples);
    private int TrackStart(int track) => _bus.TrackStart(track);
    private int TrackEnd(int track) => _bus.TrackEnd(track);
    private int TrackType(int track) => _bus.TrackType(track);
    private int TrackLoopEnabled(int track) => _bus.TrackLoopEnabled(track);
    private int TrackLoopOffset(int track) => _bus.TrackLoopOffset(track);
    private void pcm_reset() => _bus.ResetPcm();
    private void cdd_reset() => _bus.ResetCdDrive();
    private void cdd_read_audio(uint samples) => _bus.ReadCdAudio(samples);
    private void cdd_seek_audio(int track, int sector) => _bus.SeekCdAudio(track, sector);
    private void cdd_read_data(ArrayPointer<byte> destination, ArrayPointer<byte> subheader) =>
        _bus.ReadCdData(destination.Bytes(0x800), subheader.IsNull ? [] : subheader.Bytes(4));
    private byte pcm_read(uint address, uint cycles) => _bus.ReadPcm(address, cycles);
    private void pcm_write(uint address, byte data, uint cycles) => _bus.WritePcm(address, data, cycles);
    private uint sram_read_byte(uint address) => sram.ReadByte(address);
    private uint sram_read_word(uint address) => sram.ReadWord(address);
    private void sram_write_byte(uint address, uint data) => sram.WriteByte(address, data);
    private void sram_write_word(uint address, uint data) => sram.WriteWord(address, data);
    private static void CopyMemory(ArrayPointer<byte> destination, ArrayPointer<byte> source, int length) => source.Bytes(length).CopyTo(destination.Bytes(length));

    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("MegaSD context buffer is too short.", nameof(state));
        state[..NativeContextSize].Clear();
        WriteT_MEGASD_HW(megasd_hw, state);
        int offset = NativeContextSize;
        if (system_hw != 0x84)
        {
            offset += _bus.SaveCdContext(state[offset..]);
            offset += _bus.SavePcmContext(state[offset..]);
            state[offset++] = cd_control;
        }
        return offset;
    }
    public int LoadContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("MegaSD context buffer is too short.", nameof(state));
        ReadT_MEGASD_HW(megasd_hw, state);
        int offset = NativeContextSize;
        if (system_hw != 0x84)
        {
            offset += _bus.LoadCdContext(state[offset..], "GENPLUS-GX 1.7.6");
            offset += _bus.LoadPcmContext(state[offset..]);
            cd_control = state[offset++];
        }
        if (megasd_hw.playbackLoopTrack >= track_count)
            megasd_hw.playbackLoopTrack = (byte)(track_count != 0 ? track_count - 1 : 0);
        if (megasd_hw.playbackEndTrack >= track_count)
            megasd_hw.playbackEndTrack = (byte)(track_count != 0 ? track_count - 1 : 0);
        return offset;
    }
}
