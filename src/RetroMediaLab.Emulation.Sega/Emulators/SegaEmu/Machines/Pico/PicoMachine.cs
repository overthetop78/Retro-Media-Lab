using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;
using PicoPcm = RetroMediaLab.Chips.Audio.Sega.PicoAdpcm.PicoAdpcm;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.Pico;

internal sealed partial class PicoMachine(MachineResources resources) : SegaBoard(SystemHardware.Pico,resources)
{
    private PicoPcm _pcm = null!;
    public override byte PicoPage { get; set; }
    protected override ReadOnlySpan<byte> StateSignature => "SEGAEMU 00000004"u8;
    protected override int MachineContextSize => 1 + PicoPcm.ContextSize;
    protected override void SaveMachineContext(Span<byte> state)
    { state[0] = PicoPage; _pcm.SaveContext(state[1..]); }
    protected override bool CanLoadMachineContext(ReadOnlySpan<byte> state) =>
        state.Length == MachineContextSize && state[0] <= 7 && PicoPcm.CanLoadContext(state[1..]);
    protected override void LoadMachineContext(ReadOnlySpan<byte> state)
    { PicoPage = state[0]; _pcm.LoadContext(state[1..]); }
    protected override void ResetMachineContext() { PicoPage = 0; _pcm.Reset(); }
    protected override void InitializeMachineBus()
    {
        InitializeMegaDriveMapping();
        _pcm = new(MachineAudioBuffer, _ => RefreshMachineInterrupt());
    }
    protected override void ConfigureSixteenBitIo() => ConfigurePicoIo(ReadByte, ReadWord, WriteByte, WriteWord);
    protected override void ConfigureZ80Ports() => UsePowerBasePorts();
    protected override Action<int> CreateFrameScheduler() => CreateMegaDriveFrameScheduler();
    protected override bool AcceptsHardware(byte hardware) => hardware == SystemHardware.Pico;
}
