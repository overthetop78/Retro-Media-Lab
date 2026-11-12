using RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Contracts;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;
using RetroMediaLab.Chips.Logic.Common.Contracts;
using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2009.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Contracts;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Contracts;

internal sealed record Sg1000BoardState(int Version, byte[] CartridgeHash, long Time,
    long NextMasterIndex, bool MasterHigh, bool VdpReleased, bool CpuReleased,
    ushort Pressed, bool Pause, CpuZ80State Cpu, Tms9918AState Vdp, Sn76489State Psg,
    CombinationalState Decoder, CombinationalState Gates, CombinationalState[] Joy,
    Tmm2009State Ram, Mb8118State[] Dram, byte[] Pixels, bool[] PixelKnown,
    long Frames, short[] Samples, long AudioTime, long SampleIndex, long SampleStart,
    double SampleArea, double PreviousSample, double DcOutput, int AudioLevel,
    Sg1000RomConfiguration RomConfiguration,byte[] InternalGameHash,long CartridgeOrigin,
    Tmm23256State? CartridgeLow,Tmm23256State? CartridgeHigh,Tmm23256State? InternalRom,
    Tmm2016State? CartridgeRam);
