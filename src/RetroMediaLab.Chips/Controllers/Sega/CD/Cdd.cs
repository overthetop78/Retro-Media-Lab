using RetroMediaLab.Chips.Audio.Common.Resampling;
using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Controllers.Sega.CD;

public interface ICddBus
{
    Register16[] Registers { get; }
    byte PendingInterrupts { get; set; }
    void UpdateSubIrq(uint mask);
    void UpdateDecoder(uint header);
    bool MegaSdEnabled { get; }
    void UpdateMegaSdCdda(uint samples);
}

/// <summary>CD drive protocol, timing, fader and subchannel registers, independent of disc container formats.</summary>
public sealed partial class Cdd : IDisposable
{
    private readonly ICddBus _bus;
    private readonly BandLimitedBuffer _output;
    private readonly CancellationToken _cancellation;
    private readonly short[] _audioScratch = new short[4096];
    private ICdDriveMedia? _media;
    private long _audioSamplePosition;
    private int _subchannelLba;
    private Register16[] scd_regs => _bus.Registers;
    private byte pending { get => _bus.PendingInterrupts; set => _bus.PendingInterrupts = value; }
    private int drive_latency = 1, _audioVolume = 100;
    private bool has_subcode => _media?.HasSubchannel == true;
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

    public Cdd(ICddBus bus, BandLimitedBuffer output, CancellationToken cancellation)
    {
        _bus = bus; _output = output; _cancellation = cancellation;
    }
    public void Mount(ICdDriveMedia media)
    {
        ArgumentNullException.ThrowIfNull(media);
        if (media.Tracks.Count is < 1 or > 99) throw new ArgumentException("A disc needs 1 to 99 tracks.", nameof(media));
        Unload();
        for (int i = 0; i < media.Tracks.Count; i++)
        {
            CdTrack source = media.Tracks[i];
            if (source.EndLba < source.StartLba || source.Type is < 0 or > 2) throw new ArgumentException("Invalid CD track.", nameof(media));
            track_t target = cdd.toc.tracks[i];
            target.start = source.StartLba; target.end = source.EndLba; target.type = source.Type;
            target.loopEnabled = source.LoopEnabled ? 1 : 0; target.loopOffset = source.LoopOffset;
        }
        cdd.toc.last = media.Tracks.Count; cdd.toc.end = media.LeadOutLba;
        cdd.toc.tracks[cdd.toc.last].start = media.LeadOutLba;
        cdd.loaded = 1; _media = media;
        SeekAudio(0, 0); Reset();
    }
    public void Unload()
    {
        // The media is borrowed: its owner closes readers and data sources after detaching it.
        _media = null; cdd.loaded = 0; cdd.toc.last = cdd.toc.end = 0;
        foreach (track_t track in cdd.toc.tracks) track.offset = track.start = track.end = track.type = track.loopEnabled = track.loopOffset = 0;
        _audioSamplePosition = 0; _subchannelLba = 0;
        Reset(); _output.Clear();
    }
    public void Initialize(int sampleRate) => _output.SetRates(44100, sampleRate);
    public void Reset() => cdd_reset();
    public void Update() => cdd_update();
    public void ProcessCommand() => cdd_process();
    public void ReadData(Span<byte> destination, Span<byte> subheader)
    {
        _cancellation.ThrowIfCancellationRequested();
        if (cdd.index >= cdd.toc.last || cdd.toc.tracks[cdd.index].type == 0 || cdd.lba < 0) return;
        _media?.ReadData(cdd.lba, destination, subheader);
    }
    public void SeekAudio(int track, int lba) => _audioSamplePosition = (lba - cdd.toc.tracks[Math.Clamp(track, 0, 99)].start) * 588L;
    private static int abs(int value) => Math.Abs(value);
    private void cdc_decoder_update(uint header) => _bus.UpdateDecoder(header);
    private void s68k_update_irq(uint mask) => _bus.UpdateSubIrq(mask);
    private void cdd_seek_audio(int track, int lba) => SeekAudio(track, lba);
    private void cdd_read_subcode() => ReadSubcode();
    private void SeekSubcode(int offset) => _subchannelLba = offset / 96;
    private void ReadSubcode()
    {
        Span<byte> subcode = stackalloc byte[96]; subcode.Clear();
        _media?.ReadSubchannel(_subchannelLba++, subcode);
        scd_regs[0x68 >> 1].Low = (byte)((scd_regs[0x68 >> 1].Low + 98) & 0x7e);
        int index = (scd_regs[0x68 >> 1].Low + 0x100) >> 1;
        for (int i = 0; i < 96; i += 2)
        {
            int code = 0;
            for (int j = 0; j < 8; j++)
            {
                int bits = (subcode[j * 12 + i / 8] >> (6 - (i & 6))) & 3;
                code |= (bits & 1) << (7 - j); code |= (bits >> 1) << (15 - j);
            }
            scd_regs[index].Word = (ushort)code; index = (index + 1) & 0xbf;
        }
        if ((scd_regs[0x32 >> 1].Low & 0x40) != 0)
        {
            pending |= 1 << 6; s68k_update_irq((uint)((pending & scd_regs[0x32 >> 1].Low) >> 1));
        }
    }
    public void UpdateAudio(uint samples)
    {
        uint clocks = (uint)_output.ClocksNeeded((int)samples);
        if (_bus.MegaSdEnabled) _bus.UpdateMegaSdCdda(clocks); else ReadAudio(clocks);
    }
    public void Dispose() => Unload();
}
