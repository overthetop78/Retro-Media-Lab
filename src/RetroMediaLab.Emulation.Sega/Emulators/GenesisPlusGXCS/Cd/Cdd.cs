using System.IO;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Sound;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal interface ICddBus
{
    Register16[] Registers { get; }
    byte PendingInterrupts { get; set; }
    Memory<byte> CdcRam { get; }
    void UpdateSubIrq(uint mask);
    void UpdateDecoder(uint header);
    bool MegaSdEnabled { get; }
    void UpdateMegaSdCdda(uint samples);
}

internal sealed partial class Cdd : IDisposable
{
    private readonly ICddBus _bus;
    private readonly BlipBuffer _output;
    private readonly Func<string, Stream?> _openFile;
    private readonly CancellationToken _cancellation;
    private Register16[] scd_regs => _bus.Registers;
    private byte pending { get => _bus.PendingInterrupts; set => _bus.PendingInterrupts = value; }
    private int drive_latency = 1;
    private int _audioVolume = 100;
    private bool has_subcode => _subcode is not null;
    public uint Cycles { get => cdd.cycles; set => cdd.cycles = value; }
    public int Loaded => cdd.loaded;
    public int Index { get => cdd.index; set => cdd.index = value; }
    public int Lba { get => cdd.lba; set => cdd.lba = value; }
    public byte Status { get => cdd.status; set => cdd.status = value; }
    public Memory<ushort> Fader => cdd.fader;
    public int TrackCount => cdd.toc.last;
    public int TrackStart(int track) => cdd.toc.tracks[track].start;
    public int TrackEnd(int track) => cdd.toc.tracks[track].end;
    public int TrackType(int track) => cdd.toc.tracks[track].type;
    public int TrackLoopEnabled(int track) => cdd.toc.tracks[track].loopEnabled;
    public int TrackLoopOffset(int track) => cdd.toc.tracks[track].loopOffset;
    public int LatencyFactor { get => drive_latency; set => drive_latency = value; }
    public int AudioVolume { get => _audioVolume; set => _audioVolume = value; }

    public Cdd(ICddBus bus, BlipBuffer output, Func<string, Stream?> openFile, CancellationToken cancellation)
    {
        _bus = bus; _output = output; _openFile = openFile; _cancellation = cancellation;
    }
    public void Initialize(int sampleRate) => _output.SetRates(44100, sampleRate);
    public void Reset() => cdd_reset();
    public void Update() => cdd_update();
    public void ProcessCommand() => cdd_process();
    private static int abs(int value) => Math.Abs(value);
    private void cdc_decoder_update(uint header) => _bus.UpdateDecoder(header);
    private void s68k_update_irq(uint mask) => _bus.UpdateSubIrq(mask);
    private void cdd_seek_audio(int track, int lba) => SeekAudio(track, lba);
    private void cdd_read_subcode() => ReadSubcode();
    private void SeekSubcode(int offset) { if (_subcode is not null) SeekStream(_subcode, offset); }
    public void UpdateAudio(uint samples)
    {
        uint clocks = unchecked((uint)_output.ClocksNeeded((int)samples));
        if (_bus.MegaSdEnabled) _bus.UpdateMegaSdCdda(clocks);
        else ReadAudio(clocks);
    }
    public void Dispose() => Unload();
}
